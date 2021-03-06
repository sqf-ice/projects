#include "ip.h"
#include "misc.h"
#include <stdio.h>
#include <errno.h>
#include <string.h>

namespace common {

namespace network {

// methods to initialize and deinitialize socket subsystem
int InitSockets()
{
#ifdef SOCKETS_WSA
#define VERSION_MINOR 1
#define VERSION_MAJOR 1

	WSADATA WsaData;
	WORD wVersionRequested = MAKEWORD(VERSION_MINOR, VERSION_MAJOR);

	int err = WSAStartup(wVersionRequested, &WsaData);
	if (err != 0)
		return 0;

	if (LOBYTE(WsaData.wVersion) != VERSION_MAJOR ||
		HIBYTE(WsaData.wVersion) != VERSION_MINOR )
	{ // We could not find a usable WinSock DLL
		WSACleanup();
		return 0;
	}
#endif
	return 1; // success
} //  InitSockets()


int FinitSockets()
{
#ifdef SOCKETS_WSA
	WSACleanup();
#endif
	return 1; // success
} //  FinitSockets()


///////////////////////////////////////////////////////////////////////////
// Class CIpSocket implementation

unsigned int cIpSocket::m_SocketCount = 0;
unsigned int cIpSocket::m_MaxSocketSeqNumber = 0;

cIpSocket::cIpSocket()
{
	m_SocketSeqNumber = m_MaxSocketSeqNumber;
	m_Socket = INVALID_SOCKET;

	m_cTOS = 0;
	m_cTTL = 128;
}

//virtual
cIpSocket::~cIpSocket()
{
	destroy();
}

void cIpSocket::storeLastErrorCode()
{
#ifdef SOCKETS_WSA
	m_LastErrorCode = WSAGetLastError();
#endif
#ifdef SOCKETS_BSD
	m_LastErrorCode = errno;
#endif
}

bool cIpSocket::create(int af, int type, int protocol)
{
    if (!m_SocketCount)
        InitSockets();
    if (isCreated())
        return true; // opened already, do nothing
    
    ++m_SocketCount;
    ++m_MaxSocketSeqNumber;
    
    m_Socket = socket(af, type, protocol);
    if (m_Socket == INVALID_SOCKET)
    {
        storeLastErrorCode();
        perror("socket");
        return false;
    }
    
    return true;
}

void cIpSocket::destroy()
{
    if (isDestroyed())
        return; // do nothing

    onBeforeClosing();
    
#ifdef SOCKETS_WSA
    ::shutdown(m_Socket, SD_BOTH);
    ::closesocket(m_Socket);
#endif
    
#ifdef SOCKETS_BSD
    ::shutdown(m_Socket, SHUT_RDWR);
    if (::close(m_Socket) != 0) // success = 0, fail = -1
    {
        perror("close");
    }
#endif
    
    m_Socket = INVALID_SOCKET;
    --m_SocketCount;
    if (!m_SocketCount)
        FinitSockets();
}

bool cIpSocket::bind(IPPORT portNo, IPADDRESS_TYPE InterfaceIpAddress/* = INADDR_ANY */)
{
	sockaddr_in local;
	local.sin_family = AF_INET;
	local.sin_addr.s_addr = InterfaceIpAddress;
	local.sin_port = htons(portNo);
	if (::bind(GetSocket(), (sockaddr *)&local, sizeof(local)) == SOCKET_ERROR)
	{
            perror("bind");
            storeLastErrorCode();
            return false;
	}
	return true;
}

bool cIpSocket::SetOption(int level, int OptionName, void *pcValue, socklen_t OptionLength)
{
	if ((setsockopt(m_Socket, level, OptionName, pcValue, OptionLength)) == SOCKET_ERROR)
	{
            perror("setsockopt");
            storeLastErrorCode();
            return false;
	}
	return true;
}

bool cIpSocket::GetOption(int level, int OptionName, void *pcValue, socklen_t &OptionLength)
{
	if ((getsockopt(m_Socket, level, OptionName, pcValue, &OptionLength)) == SOCKET_ERROR)
	{
        perror("getsockopt");
		storeLastErrorCode();
		return false;
	}
	return true;
}

unsigned long Sum16bit(unsigned short *pData, socklen_t nDataSize)
{
	unsigned long checksum = 0;

	while (nDataSize > 1)
	{
		checksum += *pData++;
		nDataSize  -= sizeof(unsigned short);
	}
	if (nDataSize)
		checksum += *(unsigned char *)pData;

	return checksum;
}

unsigned short cIpSocket::CalcCheckSum(unsigned short *pData, unsigned short nDataSize)
{
	unsigned long checksum = Sum16bit(pData, nDataSize);

	checksum =  (checksum >> 16) + (checksum & 0xFFFF);
	checksum += (checksum >> 16);

	return (unsigned short)(~checksum);
}

unsigned short cIpSocket::CalcPseudoCheckSum(SIpHeader *pIpHeader, unsigned short *pData, unsigned short nDataSize)
{
	SPseudoHeader pseudo;
	pseudo.src_addr = pIpHeader->sourceIP;
	pseudo.dst_addr = pIpHeader->destIP;
	pseudo.zero = 0;
	pseudo.proto = pIpHeader->proto;
	pseudo.length = htons(nDataSize);
	unsigned long checksum = Sum16bit((unsigned short *)&pseudo, sizeof(pseudo));
	checksum += Sum16bit(pData, nDataSize);

	checksum =  (checksum >> 16) + (checksum & 0xFFFF);
	checksum += (checksum >> 16);

	return (unsigned short)(~checksum);
}

bool cIpSocket::SetReadTimeout(unsigned int timeout)
{
    struct timeval t;
    t.tv_sec = timeout/1000;
    t.tv_usec = (timeout % 1000)*1000;
    return SetOption(SOL_SOCKET, SO_RCVTIMEO, &t, sizeof(t));
}

//virtual
bool cIpSocket::ReadFrom
(
	void *pBufferToFill,
	size_t nBufferSize,
	int &nReadBytes,
	struct sockaddr *pSrc
)
{
	socklen_t fromlen = sizeof(*pSrc);
	nReadBytes = recvfrom(GetSocket(), (char *)pBufferToFill, nBufferSize, 0, pSrc, &fromlen);
	if (nReadBytes == SOCKET_ERROR)
	{
        perror("recvfrom");
		storeLastErrorCode();
		return false;
	}
	return true;
}

//virtual
bool cIpSocket::ReadFrom
(
    void *pBufferToFill,
    size_t nBufferSize,
    int &nReadBytes,
    IPADDRESS_TYPE &IpAddress,
    IPPORT &portNo
)
{
    sSockAddrIn from;
    memset(&from, 0, sizeof(from));
    from.addrin.sin_family = AF_INET;
	bool ret = ReadFrom(pBufferToFill, nBufferSize, nReadBytes, &from.addr);
    IpAddress = getIP(&from.addr);
    portNo = ntohs(from.addrin.sin_port);
    return ret;
}


bool cIpSocket::SetWriteTimeout(unsigned int timeout)
{
    struct timeval t;
    t.tv_sec = timeout/1000;
    t.tv_usec = (timeout % 1000)*1000;
	return SetOption(SOL_SOCKET, SO_SNDTIMEO, &t, sizeof(t));
}

//virtual
bool cIpSocket::WriteTo
(
    void *pBufferToSend,
    size_t nBytesToSend,
    int &nSentBytes,
    struct sockaddr *pDest
)
{
    nSentBytes = sendto(m_Socket, (char *)pBufferToSend, nBytesToSend, 0, pDest,	sizeof(*pDest));
    if (nSentBytes == SOCKET_ERROR)
    {
        perror("sendto");
        storeLastErrorCode();
		return false;
    }
	return true;
}

//virtual
bool cIpSocket::WriteTo
(
    void *pBufferToSend,
    size_t nBytesToSend,
    int &nSentBytes,
    char *pszIpAddress,
    IPPORT portNo
)
{
    IPADDRESS_TYPE IpAddress = StringToAddress(pszIpAddress);
    return WriteTo(pBufferToSend, nBytesToSend, nSentBytes, IpAddress, portNo);
}

//virtual
bool cIpSocket::WriteTo
(
    void *pBufferToSend,
    size_t nBytesToSend,
    int &nSentBytes,
    IPADDRESS_TYPE IpAddress,
    IPPORT portNo
)
{
    sSockAddrIn dest;
    memset(&dest, 0, sizeof(dest));
    dest.addrin.sin_family = AF_INET;
    dest.addrin.sin_port = htons(portNo);
    setIP(&dest.addr, IpAddress);
    return WriteTo(pBufferToSend, nBytesToSend, nSentBytes, &dest.addr);
}

bool cIpSocket::SetSendTTL(unsigned char ttl)
{
    m_cTTL = ttl;
    int dwValue = ttl;
    return SetOption(IPPROTO_IP, IP_TTL, &dwValue, sizeof(dwValue));
}

bool cIpSocket::SetSendTOS(unsigned char tos)
{
	m_cTOS = tos;
	int dwValue = tos;
	return SetOption(IPPROTO_IP, IP_TOS, (char *)&dwValue, sizeof(dwValue));
}

bool cIpSocket::EnableBroadcasting(bool bEnable /*= true */)
{
    int dwValue = bEnable;
    return SetOption(SOL_SOCKET, SO_BROADCAST, &dwValue, sizeof(dwValue));
}

bool cIpSocket::WriteBroadcast(void *pBufferToSend, size_t nBytesToSend, int &nSentBytes, IPPORT portNo)
{
    return WriteTo(pBufferToSend, nBytesToSend, nSentBytes, INADDR_BROADCAST, portNo);
}

///////////////////////////////////////////////////////////////////////////
// Class CUserSocket implementation

cUserSocket::cUserSocket()
{
}

//virtual
cUserSocket::~cUserSocket()
{
}



} // namespace network

} // namespace common
