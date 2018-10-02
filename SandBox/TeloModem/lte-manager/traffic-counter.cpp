/*
 *  traffic-counter.cpp
 *
 *  Copyright (C) 2015-2018 Ooma Incorporated. All rights reserved.
 *
 */

#include <string.h>
#include <dirent.h>
#include <fcntl.h>
#include <sys/poll.h>

#include <algorithm>

#include "traffic-counter.h"
#include "log.h"

/////////////////////////////////////////////////////////////////////////
/// TrafficStatistics::TrafficStatistics
///
TrafficStatistics::TrafficStatistics()
    : nInputPackets_(0), nOutputPackets_(0), nInputBytes_(0), nOutputBytes_(0)
{
}

void TrafficStatistics::update(unsigned int nPacketSize, bool bInput)
{
    if (bInput)
    {
        ++nInputPackets_;
        nInputBytes_ += nPacketSize;
    }
    else
    {
        ++nOutputPackets_;
        nOutputBytes_ += nPacketSize;
    }
}

////////////////////////////////////////////////////////////////////////////////////////////
// InterfaceTrafficCounter

InterfaceTrafficCounter::InterfaceTrafficCounter(const std::string &ifaceName, IPADDRESS_TYPE IP)
    : BaseSniffer(ifaceName)
{
    teloIP_ = 0;
    subnetMask_ = 0;
    enforcedIP_ = IP;
}

bool InterfaceTrafficCounter::startListening()
{
    log_info("Start listening to interface %s", ifaceName_.c_str());
    bool bSuccess = getInterfaceAddressAndMask(ifaceName_, teloIP_, subnetMask_);
    if (bSuccess)
        bSuccess = promiscModeOn();
    if (!bSuccess)
        log_error("Could not listen to interface %s\n", ifaceName_.c_str());

    return bSuccess;
}

bool InterfaceTrafficCounter::stopListening()
{
    log_info("Stop listening to interface %s", ifaceName_.c_str());
    promiscModeOff();
    return true;
}

IPADDRESS_TYPE InterfaceTrafficCounter::getIP() const
{
    if (enforcedIP_)
        return enforcedIP_;
    return teloIP_;
}

//virtual
void InterfaceTrafficCounter::ipPacketCaptured(const SIpHeader *pIpHeader, const void *pIpPayload,  unsigned int nIpPayloadLen)
{
    if (isMyPacket(pIpHeader))
        TeloStat_.update(pIpHeader->getPacketLen(), isPacketToMe(pIpHeader));
    else
        UserStat_.update(pIpHeader->getPacketLen(), isPacketToMe(pIpHeader));
}

void InterfaceTrafficCounter::clearStatistics()
{
    TeloStat_.clear();
    UserStat_.clear();
}

///////////////////////////////////////////////////////
// TrafficCounter

TrafficCounter::TrafficCounter()
    : bListening_(false)
{
}

void TrafficCounter::addInterface(const char *pszInterfaceName)
{
    if (strchr(pszInterfaceName, '[') && strchr(pszInterfaceName, '['))
    {
        char *pszDup = strdup(pszInterfaceName);
        char *pcBackBracket = strchr(pszDup, ']');
        *pcBackBracket = 0;
        char *pcFrontBracket = strchr(pszDup, '[');
        *pcFrontBracket = 0;
        if (!isItInterfaceName(pszDup))
        {
            log_error("'%s' is not a valid interface name\n", pszDup);
            return;
        }

        char *pszIpAddress = pcFrontBracket+1;
        IPADDRESS_TYPE IP = dotNotationToAddress(pszIpAddress);
        if (!IP)
        {
            log_error("'%s' is not a valid IP-address\n", pszIpAddress);
            return;
        }

        interfaces_.emplace_back(pszDup, IP);
        free(pszDup);
    }
    else
    {
        if (!isItInterfaceName(pszInterfaceName))
        {
            log_error("'%s' is not a valid interface name\n", pszInterfaceName);
            return;
        }

        interfaces_.emplace_back(pszInterfaceName);
    }
}

bool TrafficCounter::startListening()
{
    for (auto &iface : interfaces_)
    {
        if (!iface.startListening())
            return false;
    }
    bListening_ = true;
    return true;
}

bool TrafficCounter::stopListening()
{
    for (auto &iface : interfaces_)
        iface.stopListening();
    bListening_ = false;
    return true;
}

int TrafficCounter::doJob()
{
    const int OK = 0;
    const int NotOK = 1;
    // initial check
    if (!hasJob())
        return NotOK;

    struct pollfd fds[256];
    nfds_t nfds = 0;
    memset(fds, 0, sizeof(fds));

    for (auto &iface : interfaces_)
    {
        fds[nfds].fd = iface.getSelectableFd();
        fds[nfds].events = POLLIN;
        ++nfds;
    }
    const int timeout = 10; // 10 msec

    bool bContinue = true;
    while (bContinue)
    {
        int rc = poll(fds, nfds, timeout);
        if (rc > 0)
        {
            for (nfds_t i = 0; i < nfds; ++i)
            {
                if (fds[i].revents == POLLIN)
                    interfaces_[i].waitForPacket();
                fds[i].revents = 0;
            }
            bContinue = true; // is anybody else in there?
        }
        else if (rc < 0)
        {
            perror("  poll() failed");
            bContinue = false;
        }
        else
            bContinue = false; // 0 means "timeout expired -> do nothing and exit
    }
    return OK;
}

int TrafficCounter::getUserInputBytes() const
{
    int nBytes = 0;
    for (auto &iface : interfaces_)
        nBytes += iface.getUserInputBytes();
    return nBytes;
}

int TrafficCounter::getUserOutputBytes() const
{
    int nBytes = 0;
    for (auto &iface : interfaces_)
        nBytes += iface.getUserOutputBytes();
    return nBytes;
}

int TrafficCounter::getTeloInputBytes() const
{
    int nBytes = 0;
    for (auto &iface : interfaces_)
        nBytes += iface.getTeloInputBytes();
    return nBytes;
}

int TrafficCounter::getTeloOutputBytes() const
{
    int nBytes = 0;
    for (auto &iface : interfaces_)
        nBytes += iface.getTeloOutputBytes();
    return nBytes;
}

void TrafficCounter::clearStatistics()
{
    for (auto &iface : interfaces_)
        iface.clearStatistics();
}


