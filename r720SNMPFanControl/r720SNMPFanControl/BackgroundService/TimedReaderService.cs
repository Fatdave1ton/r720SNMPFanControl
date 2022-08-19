using r720SNMPFanControl.Configs;
using r720SNMPFanControl.Enums;
using SnmpSharpNet;
using System.Diagnostics;
using System.Net;
using System.Runtime.Intrinsics.Arm;

namespace r720SNMPFanControl.BackgroundService
{
    public class TimedReaderService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<TimedReaderService> _logger;
        private Timer _timer = null!;
        private OIDs _Oids;
        private Passwords _Passwords;

        private Mode currentMode = Mode.Manual;
        private int manualPercent = 0;
        private string _baseArguments;
        private readonly string _rawArgument;

        public TimedReaderService(ILogger<TimedReaderService> logger, OIDs Oids, Passwords Passwords)
        {
            _logger = logger;
            _Oids = Oids;
            _Passwords = Passwords;
            _baseArguments = $@"C:\ipmitool_1.8.18-dellemc_p001\ipmitool -I lanplus -H {_Passwords.Hostname} -U {_Passwords.User} -P {_Passwords.Password}";
            _rawArgument = $"{_baseArguments} raw";
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("f")} - Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(2));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {

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
            string message = "";
            foreach (int rpm in readings.FanRpms)
            {
                message += $"fan {i} rpm {rpm}{Environment.NewLine}";
                i++;
            }
            i = 1;
            foreach (int temp in readings.Temps)
            {
                message += $"temp {i} rpm {temp}{Environment.NewLine}";
                i++;
            }
            i = 1;
            foreach (int cputemp in readings.CPUTemps)
            {
                message += $"CPU {i} temp {cputemp}{Environment.NewLine}";
                i++;
            }

            //_logger.LogInformation(message);
            //check actual temps against temp curve

            if (readings.CPUTemps.Any(cpu => cpu >= 45))
            {
                _logger.LogInformation($"{DateTime.Now.ToString("f")} - Switched to Auto(high temp override)");
                SwitchToAutomatic();
            }
            else
            {
                int percentNeeded = (5 - (40 - (int)readings.CPUTemps.Max()));
                if (percentNeeded < 5) { percentNeeded = 5; }

                if (percentNeeded != manualPercent)
                {
                    _logger.LogInformation($"{DateTime.Now.ToString("f")} - Switched to Manual: {percentNeeded}%");
                    SwitchToManual(percentNeeded);
                    manualPercent = percentNeeded;
                    currentMode = Mode.Manual;
                }
            }

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
        public void SwitchToAutomatic()
        {
            Command($"{_rawArgument} 0x30 0x30 0x01 0x01");
            currentMode = Mode.Automatic;
        }

        public void SwitchToManual(int speedPercent)
        {
            Command($"{_rawArgument} 0x30 0x30 0x01 0x00");
            Command($"{_rawArgument} 0x30 0x30 0x02 0xff 0x{speedPercent:x}");
            currentMode = Mode.Manual;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("f")} - Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            SwitchToAutomatic();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            SwitchToAutomatic();

            _timer?.Dispose();
        }
    }
}
