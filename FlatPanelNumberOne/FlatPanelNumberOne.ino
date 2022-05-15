//Initializing LED Pin
int PWMA = D3;
int DIRA = D12;

#define DEVICE_IDENTIFICATION "FLATONE"
#define CMD_IDENTIFICATION "ID"
#define CMD_ON "ON"
#define CMD_OFF "OF"
#define CMD_BR "BR"
#define CMD_GET_BR "RB"
#define CMD_INFO "IF"

String g_command = "";
bool g_commandComplete = false;
String g_info = "Ready";
float brightness = 0;
bool isOn = false;

void setup() { 
  pinMode(PWMA, OUTPUT);
  pinMode(DIRA, OUTPUT);
 
  digitalWrite(PWMA, LOW);
  digitalWrite(DIRA, HIGH);

  Serial.begin(9600);
}


float Extract(String cmdid, String cmdstring)
{
  cmdstring.remove(0, cmdid.length());
  cmdstring.replace(':', ' ');
  cmdstring.trim();
  return cmdstring.toFloat();
}

void Dispatcher()
{
  if(g_command.startsWith(CMD_IDENTIFICATION))
  {
    Serial.print(DEVICE_IDENTIFICATION);
    Serial.print('#');
  }
  else if(g_command.startsWith(CMD_ON))
  {
    analogWrite(PWMA, (int)brightness);
    isOn = true;
    Serial.print("1#");
  }
  else if(g_command.startsWith(CMD_OFF))
  {
    digitalWrite(PWMA, LOW);
    isOn = false;
    Serial.print("1#");
  }
  else if(g_command.startsWith(CMD_BR))
  {
    brightness = Extract(CMD_BR, g_command);
    if(isOn)
      analogWrite(PWMA, (int)brightness);
    Serial.print("1#");
  }
  else if(g_command.startsWith(CMD_GET_BR))
  {
    Serial.print(brightness);
    Serial.print('#');
  }
  else if (g_command.startsWith(CMD_INFO))
  {
    Serial.print(g_info);
    Serial.print("#");
  }
  else
    Serial.print("0#");
  
  g_command = "";
  g_commandComplete = false;
}

void loop() {
  if(g_commandComplete)
  {
    Dispatcher();
  }
}

void serialEvent() {
  while (Serial.available()) {
    // get the new byte:
    char inChar = (char)Serial.read();
    if(inChar == '\n') continue;
    if(inChar == '\r') continue;
    // add it to the inputString:
    g_command += inChar;
    // if the incoming character is a newline, set a flag so the main loop can
    // do something about it:
    if (inChar == ':') {
      g_commandComplete = true;
    }
  }
}
