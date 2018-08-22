/*
 *  config.cpp
 *
 *  Copyright (C) 2015-2018 Ooma Incorporated. All rights reserved.
 *
 */

#include <stdio.h>
#include <string.h>
#include <regex>
#include "config.h"
#include "const.h"
#include "log.h"

Configuration::Configuration(const std::string configFile)
    : configFile_(configFile)
{
    params_.emplace_back(ConfigurationParam(PSZ_DEVICE_NAME, "/dev/ttyACM0", "Serial device name, something like /dev/ttyACM0"));
    params_.emplace_back(ConfigurationParam(PSZ_TRAFFIC_INTERFACE, "eth0", "Whose traffic statistics is being accounted"));
    params_.emplace_back(ConfigurationParam(PSZ_MYXID_INTERFACE, "eth0", "Whose MAC-address is used as myx_id"));

    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_ENABLED, "true", "Conventional Kafka enabled or not?"));
    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_BROKERS, "52.53.80.222:9092", "Comma-separated list of Kafka-brokers (no blanks, please!)"));
    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_TOPIC, "lte-service", "Kafka topic"));

    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_REST_PROXY_URL, "https://ec2-50-18-80-93.us-west-1.compute.amazonaws.com", "Kafka REST proxy URL"));
    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_REST_PROXY_ENABLED, "true", "Kafka REST Proxy enabled or not?"));
    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_REST_PROXY_CERT, "<path>", "Certificate file for access to Kafka REST proxy"));
    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_REST_PROXY_KEY, "<path>", "User key file for access to Kafka REST proxy"));
    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_REST_PROXY_PASSWORD, "<password>", "User password for access to Kafka REST proxy"));
    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_REST_PROXY_VERIFY_PEER, "false", "To verify Kafka REST proxy server or not?"));
    params_.emplace_back(ConfigurationParam(PSZ_KAFKA_REST_PROXY_VERBOSE, "false", "For debug purposes"));

    params_.emplace_back(ConfigurationParam(PSZ_BASIC_QUERY_DELAY, "60", "Basic delay between subsequent queries (seconds)"));
}

bool Configuration::createDefaultFile()
{
    log_debug("Writing default configuration file %s", configFile_.c_str());
    return save();
}

bool Configuration::save()
{
    FILE *fd = fopen(configFile_.c_str(), "w");
    if (!fd)
    {
        log_error("Cannot write config file %s\n", configFile_.c_str());
        return false;
    }

    for (auto param : params_)
    {
        fprintf(fd, "# %s\n", std::get<2>(param).c_str());
        fprintf(fd, "%s=%s\n\n", std::get<0>(param).c_str(), std::get<1>(param).c_str());
    }

    fclose(fd);
    return true;
}

bool Configuration::load()
{
    FILE *fd = fopen(configFile_.c_str(), "r");
    if (!fd)
    {
        log_error("Cannot read config file %s\n", configFile_.c_str());
        return false;
    }

    std::vector<ConfigurationParam> params;
    char szBuffer[1024];
    while (1)
    {
        if (fscanf(fd, "%s\n", szBuffer) != 1)
            break;
        char *pszEqual = strchr(szBuffer, '=');
        if (!pszEqual)
            continue;
        *pszEqual = 0;
        char *pszKey = szBuffer;
        char *pszValue = pszEqual+1;
        for (auto param : params_)
        {
            std::string &paramKey = std::get<0>(param);
            if (!paramKey.compare(pszKey))
            {
                const std::string paramValue = std::string(pszValue);
                const std::string &paramDescription = std::get<2>(param);
                params.emplace_back(ConfigurationParam(paramKey, paramValue, paramDescription));
            }
        }
    }
    params.swap(params_);
    fclose(fd);
    return true;
}

std::string Configuration::get(const char *pszKey, const char *pszDefaultValue) const
{
    // linear search as there is no millions of enties there
    for (auto param : params_)
    {
        const std::string &key = std::get<0>(param);
        //std::string &value = std::get<1>(param);
        //printf("%s ===== %s\n", key.c_str(), value.c_str());
        if (!key.compare(pszKey))
            return std::get<1>(param);
    }
    return std::string(pszDefaultValue);
}

bool Configuration::getBoolean(const char *pszKey, const char *pszDefaultValue) const
{
    std::string value = get(pszKey, pszDefaultValue);
    std::transform(value.begin(), value.end(), value.begin(), ::tolower);
    return (value.compare("enable") || value.compare("true"));

}
