/*
 *  lte-manager.cpp
 *
 *  Copyright (C) 2015-2018 Ooma Incorporated. All rights reserved.
 *
 */

#include <stdio.h>
#include <unistd.h>
#include <string.h>

#include <librdkafka/rdkafkacpp.h>

#include "lte-parameters.h"
#include "traffic-counter.h"
#include "config.h"
#include "const.h"
#include "log.h"

const char *PSZ_VERSION = "1";

int main(int argc, char *argv[])
{
    log_init(1);
    log_level(1);

    Configuration cfg(PSZ_CFG_FILE_PATH);
    if (!cfg.load())
        cfg.createDefaultFile();

    std::string basicDelayString = cfg.get(PSZ_BASIC_QUERY_DELAY, "60");
    unsigned int basicDelay = (unsigned int)atoi(basicDelayString.c_str());
    if (basicDelay < 10)
        basicDelay = 10;
    if (basicDelay > 7200)
        basicDelay = 7200;
    log_info("Basic delay %d seconds", basicDelay);

    // get myxID
    std::string myxInterfaceName = cfg.get(PSZ_MYXID_INTERFACE, "eth0");
    unsigned char ucMacAddress[8];
    if (!getInterfaceMacAddress(myxInterfaceName, ucMacAddress))
    {
        log_error("Cannot get MYX_ID as MAC-address of interface %s", myxInterfaceName.c_str());
        return 1;
    }
    std::string myxID;
    char szTmp[8];
    sprintf(szTmp, "%X", ucMacAddress[3]);
    myxID += szTmp;
    sprintf(szTmp, "%X", ucMacAddress[4]);
    myxID += szTmp;
    sprintf(szTmp, "%X", ucMacAddress[5]-1);
    myxID += szTmp;

    std::string deviceName = cfg.get(PSZ_DEVICE_NAME, "/dev/ttyACM0");
    ModemGTC modem(deviceName);

    std::string trafficInterfaceName = cfg.get(PSZ_TRAFFIC_INTERFACE, "usbnet0");
    TrafficCounter trafficCounter;
    trafficCounter.addInterface(trafficInterfaceName.c_str());
    if (!trafficCounter.startListening())
    {
        log_error("Cannot start listening traffic of %s", trafficInterfaceName.c_str());
        return 1;
    }

    std::string kafkaBrokers = cfg.get(PSZ_KAFKA_BROKERS, "13.57.155.153:9092");
    log_info("Kafka brokers: %s", kafkaBrokers.c_str());
    std::string errstr;
    std::string kafkaTopic = cfg.get(PSZ_KAFKA_TOPIC, "lte-service");
    log_info("Kafka topic: %s\n", kafkaTopic.c_str());
    int32_t partition = RdKafka::Topic::PARTITION_UA;

    RdKafka::Conf *conf = RdKafka::Conf::create(RdKafka::Conf::CONF_GLOBAL);
    RdKafka::Conf *tconf = RdKafka::Conf::create(RdKafka::Conf::CONF_TOPIC);

    conf->set("metadata.broker.list", kafkaBrokers, errstr);

    RdKafka::Producer *producer = RdKafka::Producer::create(conf, errstr);
    if (!producer)
    {
        log_error("Failed to create producer: %s\n", errstr.c_str());
        return 1;
    }

    RdKafka::Topic *pTopic = RdKafka::Topic::create(producer, kafkaTopic, tconf, errstr);
    if (!pTopic)
    {
        log_error("Failed to create topic: %s", errstr.c_str());
        return 1;
    }

    // to daemonize myself, or not to daemonize?
    // quick and dirty - just iterate through the list of cmd-lene args looking for -d option, meaning daemonizing.
    // TODO: make something better with ehhanced and sophisticated cmd-line processing
    bool bDaemonize = false;
    for (int i = 1; i < argc; ++i)
    {
        if (strcasecmp(argv[i], "-d") == 0)
            bDaemonize = true;
        else
        { //  many options together like -abcdefgh possible in the future
            if (argv[i][0] == '-')
            {
                if (strchr(argv[i], 'd') || strchr(argv[i], 'D'))
                    bDaemonize = true;
            }
        }
    }
    if (bDaemonize)
    {
        int errorCode = daemon(1, 1); // first '1' - do not change working dir, second '1' - leave standard file descriptors "as is"
        if (errorCode != 0)
        {
            log_error("Failed to daemonise itself");
            return 1;
        }
    }
    // else ordinary program


    std::vector<LteParameterGroup *> allGroups;
    allGroups.emplace_back(new ModemControlParameterGroup(modem));
    allGroups.emplace_back(new ConstantModemParameterGroup(modem));
    allGroups.emplace_back(new VariableModemParameterGroup(modem));
    allGroups.emplace_back(new NetworkParameterGroup(trafficInterfaceName));
    allGroups.emplace_back(new TrafficParameterGroup(trafficCounter));

    bool bNeedToContinue = true;
    JsonContent queryResult;

    while (bNeedToContinue)
    {
        trafficCounter.doJob();

        queryResult.clear();

        for (auto pGroup : allGroups)
            pGroup->get(basicDelay, queryResult);

        if (!queryResult.empty())
        {
            queryResult.emplace_back(KeyValue("version", PSZ_VERSION));
            queryResult.emplace_back(KeyValue("myx_id", myxID));
            std::string json;
            toJSON(queryResult, json);
            printf("%s\n", json.c_str());
            producer->produce(pTopic, partition,
                    RdKafka::Producer::RK_MSG_COPY,
                    (char *)json.c_str(), json.size(),
                    NULL, NULL);
        }
        sleep(10);
        bNeedToContinue = true; // TODO: check for interruption
    }

    for (auto pGroup : allGroups)
        delete pGroup;

    allGroups.clear();

    return 0;
}

// TELO KILLER
/*
int main(int argc, char *argv[])
{
    printf("argc = %d\n", argc);
    if (argc < 2)
    {
        printf("USAGE: %s <UsbDeviceName> <timeout_in_seconds>\n", argv[0]);
        return 1;
    }

    char *pszDevice = argv[1];
    int timeout = 1;
    if (argc > 2)
        timeout = atoi(argv[2]);

    int nCount = 0;
    bool bSuccess = true;

    while (bSuccess)
    {
        printf("opening...");
        int fd = ::open(pszDevice, O_RDWR | O_NOCTTY);
        if (fd < 0)
        {
            fprintf(stderr, "error, counldn't open file %s\n", pszDevice);
            perror("open");
            return 1;
        }
        printf("done successfully\n");
        printf("sleeping %d seconds before closing\n", timeout);
        sleep(timeout);

        printf("closing...");
        ::close(fd);
        printf("closed\n");

        printf("\t***** %d times done *****\n", ++nCount);
        printf("sleeping %d seconds before re-opening\n", timeout);
        sleep(timeout);
    }

    return 0;
}
*/

