#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>


const char* ssid = "Lamborghini";
const char* password = "ghj82RTp";

const char* url = "https://4f7ahkyqt26wng75ossxz4ndae0rvpsb.lambda-url.eu-central-1.on.aws/?Id=V61-45";
const char* host = "https://4f7ahkyqt26wng75ossxz4ndae0rvpsb.lambda-url.eu-central-1.on.aws";
const int httpsPort = 443;
// const String fingerprint_str = "51 87 10 92 52 38 41 A9 SD 23 V3 23 72 58 SE EF 06 8D 35 4C";
// const uint8_t* fingerprint = reinterpret_cast<const uint8_t*>(fingerprint_str.c_str());
//const char fingerprint[] PROGMEM = "63 06 40 C6 CF DE 77 13 7E 68 D5 E7 A7 29 DE 8E 88 1F 58 CE";

const char fingerprint[] PROGMEM = "CD 50 05 4F F8 6C E8 F7 6E DE 21 8E 80 93 2C 96 80 87 0A E3";

void setup() {
  unsigned short count = 0;

  Serial.begin(115200);

  Serial.println();

  Serial.print("Connecting to ");
  Serial.println(ssid);

  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    count++;

    if (count >= 30)
      wifiRestart();
  }

  Serial.println("");
  Serial.println("WiFi connected");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());
}


void wifiRestart() {
  Serial.println("Turning WiFi off...");
  WiFi.mode(WIFI_OFF);
  Serial.println("Sleeping for 10 seconds...");
  delay(10000);
  Serial.println("Trying to connect to WiFi...");
  WiFi.mode(WIFI_STA);
}


void loop() {
  Serial.println("PingLightDetector v3");
  short response_code = 0;

  // wait for WiFi connection
  if ((WiFi.status() == WL_CONNECTED)) {
    HTTPClient http;

    WiFiClientSecure httpsClient;    //Declare object of class WiFiClient

    Serial.println(url);

    Serial.printf("Using fingerprint '%s'\n", fingerprint);
    httpsClient.setFingerprint(fingerprint);
    httpsClient.setTimeout(15000); // 15 Seconds
    delay(1000);

    Serial.print("HTTPS Connecting");
    int r=0; //retry counter
    while((!httpsClient.connect(host, httpsPort)) && (r < 30)){
        delay(1000);
        Serial.print(".");
        r++;
    }
    if(r==30) {
      Serial.println("Connection failed");
    }
    else {
      Serial.println("Connected to web");
    }

    Serial.print("[HTTPS] begin...\n");
    http.begin(httpsClient, url);
    http.addHeader("Content-Type", "application/json");

    while(true){
      int httpCode = http.POST("{\"hash\": \"34jh34uh3v\"}");
      if (httpCode > 0) {
        http.writeToStream(&Serial);

        // HTTP header has been send and Server response header has been handled
        Serial.printf("[HTTP] ... code: %d\n", httpCode);

        // file found at server
        if (httpCode >= 200 and httpCode <= 299) {
          response_code = 1;
          String payload = http.getString();
          Serial.println(payload);
        }
      } else {
        Serial.printf("[HTTP] ... failed, error: %s\n", http.errorToString(httpCode).c_str());
        String payload = http.getString();
        Serial.println(payload);
      }

      delay(30000); //30 sec
    }

    http.end();
  } else {
    wifiRestart();
  }

  if (response_code) {
    Serial.println("All fine. Going to sleep...");
    ESP.deepSleep(0);
  }
}