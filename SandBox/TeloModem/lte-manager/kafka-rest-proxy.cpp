/*
 *  messaging-https.cpp
 *
 *  Copyright (C) 2015-2018 Ooma Incorporated. All rights reserved.
 *
 */

#include "kafka-rest-proxy.h"
#include "const.h"

//static
KafkaRestProxy &KafkaRestProxy::instance()
{
    static KafkaRestProxy s;
    return s;
}

KafkaRestProxy::KafkaRestProxy()
{
    bEnabled_ = true;
    bVerbose_ = false;
    bVerifyPeer_ = false;
    curl_global_init(CURL_GLOBAL_SSL);
}

//virtual
KafkaRestProxy::~KafkaRestProxy()
{
    curl_global_cleanup();
}

bool KafkaRestProxy::post(const std::string &data)
{
    bool bSuccess = false;
    CURL *pCurl = initCurl();
    if (pCurl)
    {
        curl_easy_setopt(pCurl, CURLOPT_POST, 1);

        setCommonOptions(pCurl);

        curl_easy_setopt(pCurl, CURLOPT_URL, URL_.c_str());

        curl_easy_setopt(pCurl, CURLOPT_POSTFIELDS, data.c_str());

        // header
        struct curl_slist *pCurlHeader = nullptr;
        pCurlHeader = curl_slist_append(pCurlHeader, "Content-Type: application/vnd.kafka.json.v1+json");
        curl_easy_setopt(pCurl, CURLOPT_HTTPHEADER, pCurlHeader);

        /* Perform the request, res will get the return code */
        int res = curl_easy_perform(pCurl);
        bSuccess = (res == CURLE_OK);
        // Check for errors

        curl_easy_cleanup(pCurl);
        curl_slist_free_all(pCurlHeader);
        //fprintf(stderr, "res = %d\n", res);
    }
    return bSuccess;
}

CURL *KafkaRestProxy::initCurl()
{
    CURL *pCurl = curl_easy_init();
    if (!pCurl)
        fprintf(stderr, "couldn't init curl\n");
    return pCurl;
}

void KafkaRestProxy::setCommonOptions(CURL *pCurl)
{
    curl_easy_setopt(pCurl, CURLOPT_VERBOSE, bVerbose_);

    // CERT
    printf("CERT = %s\n", certFile_.c_str());
    if (!certFile_.empty())
        curl_easy_setopt(pCurl, CURLOPT_SSLCERT, certFile_.c_str());

    // KEY
    printf("KEY = %s\n", keyFile_.c_str());
    if (!keyFile_.empty())
        curl_easy_setopt(pCurl, CURLOPT_SSLKEY, keyFile_.c_str());

    // PassPhrase
    printf("PASS = %s\n", passPhrase_.c_str());
    if (!passPhrase_.empty())
        curl_easy_setopt(pCurl, CURLOPT_KEYPASSWD, passPhrase_.c_str());

    // CA path (if enabled)
    //curl_easy_setopt(pCurl, CURLOPT_CAPATH, pPath);
    //curl_easy_setopt(curl, CURLOPT_CAINFO, "/etc/certs/cabundle.pem");

    curl_easy_setopt(pCurl, CURLOPT_SSL_VERIFYPEER, 0);
   // curl_easy_setopt(pCurl, CURLOPT_SSLVERSION, CURL_SSLVERSION_TLSv1_2);
}

void KafkaRestProxy::configure(const Configuration &cfg)
{
    bEnabled_ = cfg.getBoolean(PSZ_KAFKA_REST_PROXY_ENABLED, "true");

    URL_ = cfg.get(PSZ_KAFKA_REST_PROXY_URL, "https://ec2-50-18-80-93.us-west-1.compute.amazonaws.com");
    URL_ += "/topics/";
    URL_ += cfg.get(PSZ_KAFKA_TOPIC, "lte-manager");

    certFile_ = cfg.get(PSZ_KAFKA_REST_PROXY_CERT, "");
    keyFile_ = cfg.get(PSZ_KAFKA_REST_PROXY_KEY, "");
    passPhrase_ = cfg.get(PSZ_KAFKA_REST_PROXY_PASSWORD, "");
    bVerifyPeer_ = cfg.getBoolean(PSZ_KAFKA_REST_PROXY_VERIFY_PEER, "false");

    bVerbose_ = cfg.getBoolean(PSZ_KAFKA_REST_PROXY_VERBOSE, "false");
}

