using SnmpSharpNet;

namespace r720SNMPFanControl.BackgroundService
{

    public class Readings
    {
        private readonly ILogger<TimedReaderService> _logger;
        public Readings(ILogger<TimedReaderService> logger)
        {
            _logger = logger;

            FanRpms = new();
            Temps = new();
            CPUTemps = new();
        }
        public List<int> FanRpms { get; set; }
        public int AvgFanSpeed => FanRpms.Sum(a => a) / FanRpms.Count();

        public List<decimal> Temps { get; set; }
        public List<decimal> CPUTemps { get; set; }

        public void AddCpuReadings(SnmpV1Packet SnmpV1Packet)
        {
            if (SnmpV1Packet != null)
            {
                if (SnmpV1Packet.Pdu.ErrorStatus != 0)
                {

                    _logger.LogError("Error in SNMP reply. Error {0} index {1}",
                        SnmpV1Packet.Pdu.ErrorStatus,
                        SnmpV1Packet.Pdu.ErrorIndex);
                }
                else
                {
                    for (int i = 0; i < SnmpV1Packet.Pdu.VbList.Count();)
                    {
                        if (int.TryParse(SnmpV1Packet.Pdu.VbList[i].Value.ToString(), out int reading))
                        {
                            CPUTemps.Add((decimal)reading/10);
                        }
                        i++;
                    }
                }
            }
        }

        public void AddTempReadings(SnmpV1Packet SnmpV1Packet)
        {
            if (SnmpV1Packet != null)
            {
                if (SnmpV1Packet.Pdu.ErrorStatus != 0)
                {

                    _logger.LogError("Error in SNMP reply. Error {0} index {1}",
                        SnmpV1Packet.Pdu.ErrorStatus,
                        SnmpV1Packet.Pdu.ErrorIndex);
                }
                else
                {
                    for (int i = 0; i < SnmpV1Packet.Pdu.VbList.Count();)
                    {
                        if (int.TryParse(SnmpV1Packet.Pdu.VbList[i].Value.ToString(), out int reading))
                        {
                            Temps.Add((decimal)reading/10);
                        }
                        i++;
                    }
                }
            }
        }

        public void AddFanReadings(SnmpV1Packet SnmpV1Packet)
        {
            if (SnmpV1Packet != null)
            {
                if (SnmpV1Packet.Pdu.ErrorStatus != 0)
                {

                    _logger.LogError("Error in SNMP reply. Error {0} index {1}",
                        SnmpV1Packet.Pdu.ErrorStatus,
                        SnmpV1Packet.Pdu.ErrorIndex);
                }
                else
                {
                    for (int i = 0; i < SnmpV1Packet.Pdu.VbList.Count();)
                    {
                        if (int.TryParse(SnmpV1Packet.Pdu.VbList[i].Value.ToString(), out int reading))
                        {
                            FanRpms.Add(reading);
                        }
                        i++;
                    }
                }
            }
        }
    }
}
