cd OpenNos.Master.Server\bin\Release
start OpenNos.Master.Server.exe
timeout 10
:: edit to have wanted amount of world servers,
:: dont forget to wait about 5 seconds before starting next world server
cd OpenNos.World\bin\Release
start OpenNos.World.exe
timeout 20
cd OpenNos.Login\bin\Release
start OpenNos.Login.exe
exit