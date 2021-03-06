#include "raw.h"
#include <string.h>

namespace common {

namespace network {

///////////////////////////////////////////////////////////////////////////
// Class CIpRawSocket implementation

cIpRawSocket::cIpRawSocket()
{
        ClearIpHeader();
        m_uSrcIP = 0; // unknown yet
}

//virtual
cIpRawSocket::~cIpRawSocket()
{
}

//virtual
bool cIpRawSocket::ReadFrom
(
        void *pBufferToFill,
        size_t nBufferSize,
        int &nReadBytes,
        struct sockaddr *pSrc
)
{
        ClearIpHeader();
		bool bSuccess = inherited::ReadFrom(pBufferToFill, nBufferSize, nReadBytes, pSrc);
        if (bSuccess)
                ExtractIPHeader(pBufferToFill);
        return bSuccess;
}

//virtual
bool cIpRawSocket::ReadFrom
(
        void *pBufferToFill,
        size_t nBufferSize,
        int &nReadBytes,
        IPADDRESS_TYPE &IpAddress,
        unsigned short &PortNo
)
{
        ClearIpHeader();
		bool bSuccess = inherited::ReadFrom(pBufferToFill, nBufferSize, nReadBytes, IpAddress, PortNo);
        if (bSuccess)
                ExtractIPHeader(pBufferToFill);
        return bSuccess;
}

void cIpRawSocket::ClearIpHeader()
{
        memset(&m_IPHeader, 0, sizeof(m_IPHeader));
}

void cIpRawSocket::ExtractIPHeader(void *pBuffer)
{
        ClearIpHeader();
        SIpHeader *pSrcIpHeader = (SIpHeader *)pBuffer;
        memcpy(&m_IPHeader, pSrcIpHeader, sizeof(m_IPHeader));
}

bool cIpRawSocket::SetIpHeaderIncl(bool bIncludeIpHeader)
{
        int dwValue = bIncludeIpHeader;
        return SetOption(IPPROTO_IP, IP_HDRINCL, (char *)&dwValue, sizeof(dwValue));
}

// Fills IP-header of the packet.
// Called for sockets which have their own IP-header.
// Returns length of IP-header + options field length in octets
int cIpRawSocket::FillIpHeader(SIpHeader *pIpHeader, int nPacketSize, unsigned long uSrcIP, unsigned long uDstIP)
{
        if (!HasOwnIpHeader())
        {
//		TRACE("CIpRawSocket::FillIpHeader is called for the socket having no its own IP-header!\n");
                return 0;
        }
        pIpHeader->version = 4; //IPv4
        int nIpHeaderLength = (sizeof(SIpHeader) + FillIpHeaderOptions(pIpHeader)); // in octets
        pIpHeader->h_len = nIpHeaderLength /4; // in 32-bits words
        pIpHeader->tos = IpHeader_TOS();
        pIpHeader->total_len = htons(nPacketSize);
        pIpHeader->ident = 0;
        pIpHeader->frag_and_flags = 0;
        pIpHeader->ttl = IpHeader_TTL();
        pIpHeader->proto = IpHeader_Proto();
        pIpHeader->checksum = 0;
        pIpHeader->destIP = uDstIP;

        // look for Src IP-address
        if (uSrcIP == 0)
                uSrcIP = m_uSrcIP;
        if (uSrcIP == 0)
        {
                char szName[255];
                gethostname(szName, sizeof(szName));
                struct hostent *pHostent = gethostbyname(szName);
                char **p = pHostent->h_addr_list;
                m_uSrcIP = uSrcIP = *(IPADDRESS_TYPE *)*p;
        }
        pIpHeader->sourceIP =  uSrcIP;
        pIpHeader->checksum = CalcCheckSum((unsigned short *)pIpHeader, nIpHeaderLength);
        return nIpHeaderLength;
}


} // namespace network

} // namespace common
