using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using ACBr.Net.Core.Extensions;
using ACBr.Net.DFe.Core.Common;
using Newtonsoft.Json;

namespace ACBr.Net.NFSe.Providers
{
    internal sealed class EmissorNacionalServiceClient : IServiceClient
    {
        private readonly ProviderEmissorNacional provider;
        private readonly TipoUrl tipoUrl;
        public string EnvelopeEnvio { get; private set; }
        public string EnvelopeRetorno { get; private set; }

        #region Constructors

        public EmissorNacionalServiceClient(ProviderEmissorNacional provider, TipoUrl tipoUrl, X509Certificate2 certificado)
        {
            this.provider = provider;
            this.tipoUrl = tipoUrl;
            // if (!(Endpoint?.Binding is BasicHttpBinding binding))
            //     return;

            // binding.MaxReceivedMessageSize = int.MaxValue;
            //binding.MaxBufferPoolSize = int.MaxValue;
            //binding.MaxBufferSize = int.MaxValue;
        }

        public EmissorNacionalServiceClient(ProviderEmissorNacional provider, TipoUrl tipoUrl)
        {
            this.provider = provider;
            this.tipoUrl = tipoUrl;
            // if (!(Endpoint?.Binding is BasicHttpBinding binding))
            //     return;
            //
            // binding.MaxReceivedMessageSize = int.MaxValue;
            //binding.MaxBufferPoolSize = int.MaxValue;
            //binding.MaxBufferSize = int.MaxValue;
        }

        #endregion Constructors

        #region Methods

        public string Enviar(string cabec, string msg)
        {
            // var message = new StringBuilder();
            // message.Append("<v2:RecepcionarLoteRps soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            // message.Append("<XML xsi:type=\"xsd:string\">");
            // message.AppendEnvio(msg);
            // message.Append("</XML>");
            // message.Append("</v2:RecepcionarLoteRps>");

            return Execute("RecepcionarLoteRps", msg, "EnviarLoteRpsResponse");
        }

        public string EnviarSincrono(string cabec, string msg)
        {
            var message = new StringBuilder();
            message.Append("<v2:RecepcionarLoteRps soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            message.Append("<XML xsi:type=\"xsd:string\">");
            message.AppendEnvio(msg);
            message.Append("</XML>");
            message.Append("</v2:RecepcionarLoteRps>");

            return Execute("RecepcionarLoteRps", message.ToString(), "EnviarLoteRpsResponse");
        }

        public string ConsultarSituacao(string cabec, string msg)
        {
            throw new NotImplementedException();
        }

        public string ConsultarLoteRps(string cabec, string msg)
        {
            var message = new StringBuilder();
            message.Append("<v2:ConsultarLoteRps soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            message.Append("<xml xsi:type=\"xsd:string\">");
            message.AppendEnvio(msg);
            message.Append("</xml>");
            message.Append("</v2:ConsultarLoteRps>");

            return Execute("ConsultarLoteRps", message.ToString(), "ConsultarLoteRpsResponse");
        }

        public string ConsultarSequencialRps(string cabec, string msg)
        {
            throw new NotImplementedException();
        }

        public string ConsultarNFSeRps(string cabec, string msg)
        {
            var message = new StringBuilder();
            message.Append("<v2:ConsultarNfseRps soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            message.Append("<xml xsi:type=\"xsd:string\">");
            message.AppendEnvio(msg);
            message.Append("</xml>");
            message.Append("</v2:ConsultarNfseRps>");

            return Execute("ConsultarNfseRps", message.ToString(), "ConsultarNfseRpsResponse");
        }

        public string ConsultarNFSe(string cabec, string msg)
        {
            var message = new StringBuilder();
            message.Append("<v2:ConsultarNfseFaixa soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            message.Append("<xml xsi:type=\"xsd:string\">");
            message.AppendEnvio(msg);
            message.Append("</xml>");
            message.Append("</v2:ConsultarNfseFaixa>");

            return Execute("ConsultarNfseFaixa", message.ToString(), "ConsultarNfseFaixaResponse");
        }

        public string CancelarNFSe(string cabec, string msg)
        {
            var message = new StringBuilder();
            message.Append("<v2:CancelarNfse soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            message.Append("<xml xsi:type=\"xsd:string\">");
            message.AppendEnvio(msg);
            message.Append("</xml>");
            message.Append("</v2:CancelarNfse>");

            return Execute("CancelarNfse", message.ToString(), "CancelarNfseResponse");
        }

        public string CancelarNFSeLote(string cabec, string msg)
        {
            throw new NotImplementedException();
        }

        public string SubstituirNFSe(string cabec, string msg)
        {
            var message = new StringBuilder();
            message.Append("<v2:SubstituirNfse soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            message.Append("<xml xsi:type=\"xsd:string\">");
            message.AppendEnvio(msg);
            message.Append("</xml>");
            message.Append("</v2:SubstituirNfse>");

            return Execute("SubstituirNfse", message.ToString(), "SubstituirNfseResponse");
        }

        private string Execute(string action, string message, params string[] responseTag)
        {
            message = $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n{message}";
            EnvelopeEnvio = CompressToBase64(message);

            string url;
            switch (provider.Configuracoes.WebServices.Ambiente)
            {
                case DFeTipoAmbiente.Producao:
                    url = "https://sefin.nfse.gov.br/SefinNacional/nfse";
                    break;
                case DFeTipoAmbiente.Homologacao:
                    url = "https://sefin.producaorestrita.nfse.gov.br/SefinNacional/nfse";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(provider.Certificado);

            using (var http = new HttpClient(handler))
            {
                var json = JsonConvert.SerializeObject(new
                {
                    dpsXmlGZipB64 = EnvelopeEnvio
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = http.PostAsync(url, content).Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;
                EnvelopeRetorno = responseBody;

                var retornoEmissorNacional = JsonConvert.DeserializeObject<RetornoEmissorNacional>(responseBody);
                if (!string.IsNullOrWhiteSpace(retornoEmissorNacional?.NfseXmlGZipB64))
                {
                    var xmlRetorno = DecompressFromBase64(retornoEmissorNacional.NfseXmlGZipB64);
                    return xmlRetorno;
                }
            }

            return string.Empty;
        }
        
        public void Dispose()
        {
            // throw new NotImplementedException();
        }
        
        private static string CompressToBase64(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionLevel.Optimal, true))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }

                return Convert.ToBase64String(output.ToArray());
            }
        }
        
        private static string DecompressFromBase64(string base64)
        {
            var compressedBytes = Convert.FromBase64String(base64);

            using (var input = new MemoryStream(compressedBytes))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }

        #endregion Methods
    }

    public class RetornoEmissorNacional
    {
        [JsonProperty("tipoAmbiente")]
        public int TipoAmbiente { get; set; }
        
        [JsonProperty("versaoAplicativo")]
        public string VersaoAplicativo { get; set; }
        
        [JsonProperty("dataHoraProcessamento")]
        public DateTime DataHoraProcessamento { get; set; }
        
        [JsonProperty("idDps")]
        public string IdDps { get; set; }
        
        [JsonProperty("chaveAcesso")]
        public string ChaveAcesso { get; set; }
        
        [JsonProperty("nfseXmlGZipB64")]
        public string NfseXmlGZipB64 { get; set; }
        
        // [JsonProperty("alertas")]
        // public string Alertas { get; set; } // TODO
    }
}