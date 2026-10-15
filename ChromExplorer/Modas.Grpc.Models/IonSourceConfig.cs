namespace Modas.Grpc.Models
{
    public class IonSourceConfig
    {
        public IonSourceConfig()
        {

        }

        public double SprayVoltage { get; set; } = 0;

        public DsapHeaterConfig DsapHeaterConfig { get; set; } = new DsapHeaterConfig();
    }
}