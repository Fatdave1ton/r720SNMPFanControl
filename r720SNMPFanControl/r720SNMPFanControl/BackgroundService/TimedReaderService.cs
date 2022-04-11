using r720SNMPFanControl.Configs;
using SnmpSharpNet;
using System.Diagnostics;
using System.Net;

namespace r720SNMPFanControl.BackgroundService
{
    public class TimedReaderService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<TimedReaderService> _logger;
        private Timer _timer = null!;
        private OIDs _Oids;
        private Passwords _Passwords;

        private string _baseArguments;
        private readonly string _rawArgument;
        private readonly string _sensorList;

        public TimedReaderService(ILogger<TimedReaderService> logger, OIDs Oids, Passwords Passwords)
        {
            _logger = logger;
            _Oids = Oids;
            _Passwords = Passwords;
            _baseArguments = $@"C:\ipmitool_1.8.18-dellemc_p001\ipmitool -I lanplus -H {_Passwords.Hostname} -U {_Passwords.User} -P {_Passwords.Password}";
            _rawArgument = $"{_baseArguments} raw";
            _sensorList = $"{_baseArguments} sensor list";
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(2));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);

            OctetString community = new OctetString("DaveHome");
            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 1 (or 2)
            param.Version = SnmpVersion.Ver1;
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address
            IpAddress agent = new IpAddress("192.168.1.15");
            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, 161, 2000, 1);
            // Pdu class used for all requests
            Pdu Temps = new Pdu(PduType.Get);
            Pdu CPUTemps = new Pdu(PduType.Get);
            Pdu FanRpms = new Pdu(PduType.Get);

            foreach (string fan in _Oids.Fans)
            {
                FanRpms.VbList.Add(fan);
            }
            foreach (string temperature in _Oids.Temperatures)
            {
                Temps.VbList.Add(temperature);
            }
            foreach (string temperature in _Oids.CPUTemperatures)
            {
                CPUTemps.VbList.Add(temperature);
            }

            Readings readings = new(_logger);

            readings.AddCpuReadings((SnmpV1Packet)target.Request(CPUTemps, param));
            readings.AddTempReadings((SnmpV1Packet)target.Request(Temps, param));
            readings.AddFanReadings((SnmpV1Packet)target.Request(FanRpms, param));

            int i = 1;
            foreach (int rpm in readings.FanRpms)
            {
                _logger.LogInformation(" fan {0} rpm {1}",
                                        i,
                                        rpm);
                i++;
            }
            i = 1;
            foreach (int temp in readings.Temps)
            {
                _logger.LogInformation(" temp {0} rpm {1}",
                                        i,
                                        temp);
                i++;
            }
            i = 1;
            foreach (int cputemp in readings.CPUTemps)
            {
                _logger.LogInformation(" CPU temp {0} rpm {1}",
                                        i,
                                        cputemp);
                i++;
            }

            //check actual temps again temp curve

            //choose command for right fan speed/reset to auto

            //send command
                      

        



            target.Close();
        }
        public string Command(string arguments)
        {
            Process process = new();

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {arguments}";
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
        public void SwitchToAutomatic() => Command($"{_rawArgument} 0x30 0x30 0x01 0x01");

        public void SwitchToManual(int speedPercent)
        {
            Command($"{_rawArgument} 0x30 0x30 0x01 0x00");
            Command($"{_rawArgument} 0x30 0x30 0x02 0xff 0x{speedPercent:x}");
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
