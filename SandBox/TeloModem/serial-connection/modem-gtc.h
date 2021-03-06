#pragma once

#include <string>
#include <vector>
#include "serial-connection.h"
#include "json.h"

void convertKeyValuePairTo(JsonContent &content, std::string &json);

class ModemGTC
{
public:
    ModemGTC(const std::string &deviceName);

    bool isControllable();

    bool getManufacturerInfoRaw();
    bool getManufacturerInfo(JsonContent &content);

    bool getFirmwareVersionRaw();
    bool getFirmwareVersionInfo(JsonContent &content);

    bool getImeiRaw();
    bool getImei(JsonContent &content);

    bool getIccIdRaw();
    bool getIccId(JsonContent &content);

    bool getCarrierRaw();
    bool getCarrier(JsonContent &content);

    bool getStatusRaw();
    bool getStatus(JsonContent &content);

    bool getSPN(std::string &SPN);

protected:
    bool execute(const std::string &command);

    SerialConnection connection_;
    std::string raw_; // to avoid memory fragmentation
    JsonContent tmpContent_; // to avoid memory fragmentation
};

