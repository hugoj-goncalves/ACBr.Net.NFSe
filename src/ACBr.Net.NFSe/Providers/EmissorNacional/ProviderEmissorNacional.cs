using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using ACBr.Net.Core.Extensions;
using ACBr.Net.DFe.Core;
using ACBr.Net.DFe.Core.Common;
using ACBr.Net.DFe.Core.Serializer;
using ACBr.Net.NFSe.Configuracao;
using ACBr.Net.NFSe.Nota;

namespace ACBr.Net.NFSe.Providers
{
    internal sealed class ProviderEmissorNacional : ProviderABRASF
    {
        #region Constructors

        public ProviderEmissorNacional(ConfigNFSe config, ACBrMunicipioNFSe municipio) : base(config, municipio)
        {
            Name = "EmissorNacional";
            // Versao = "1.00";
        }

        #endregion Constructors

        #region Methods

        #region Protected Methods

        protected override void AssinarEnviar(RetornoEnviar retornoWebservice)
        {
            retornoWebservice.XmlEnvio =
                XmlSigning.AssinarXml(retornoWebservice.XmlEnvio, "DPS", "infDPS", "Id", Certificado);
        }

        protected override void AssinarEnviarSincrono(RetornoEnviar retornoWebservice)
        {
            retornoWebservice.XmlEnvio =
                XmlSigning.AssinarXml(retornoWebservice.XmlEnvio, "DPS", "infDPS", "Id", Certificado);
        }

        protected override IServiceClient GetClient(TipoUrl tipo)
        {
            return new EmissorNacionalServiceClient(this, tipo);
        }

        protected override string GetNamespace()
        {
            return "xmlns=\"http://www.sped.fazenda.gov.br/nfse\" versao=\"1.00\"";
        }

        protected override string GetSchema(TipoUrl tipo)
        {
            return "DPS_v1.01.xsd";
        }

        protected override bool PrecisaValidarSchema(TipoUrl tipo)
        {
            return true;
        }

        protected override void ValidarSchema(RetornoWebservice retorno, string schema)
        {
            var schemas = new List<string>
            {
                Path.Combine(Configuracoes.Arquivos.PathSchemas, Name, schema),
                Path.Combine(Configuracoes.Arquivos.PathSchemas, Name, "xmldsig-core-schema.xsd")
            };
            if (ValidarXml(retorno.XmlEnvio, schemas.ToArray(), out var errosSchema,
                    out var alertasSchema)) return;

            foreach (var erro in errosSchema.Select(descricao => new Evento { Codigo = "0", Descricao = descricao }))
                retorno.Erros.Add(erro);

            foreach (var alerta in
                     alertasSchema.Select(descricao => new Evento { Codigo = "0", Descricao = descricao }))
                retorno.Alertas.Add(alerta);
        }

        protected override void PrepararEnviar(RetornoEnviar retornoWebservice, NotaServicoCollection notas)
        {
            if (retornoWebservice.Lote == 0)
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Lote não informado." });
            if (notas.Count == 0)
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "RPS não informado." });
            if (notas.Count > 1)
                retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Apenas uma nota por vez." });
            if (retornoWebservice.Erros.Count > 0) return;

            var xmlLoteRps = new StringBuilder();

            foreach (var nota in notas)
            {
                var xmlRps = WriteXmlRps(nota, false, false);
                xmlLoteRps.Append(xmlRps);
                GravarRpsEmDisco(xmlRps,
                    $"Rps-{nota.IdentificacaoRps.DataEmissao:yyyyMMdd}-{nota.IdentificacaoRps.Numero}.xml",
                    nota.IdentificacaoRps.DataEmissao);
            }

            var xmlLote = new StringBuilder();
            xmlLote.Append($"<DPS {GetNamespace()}>");
            xmlLote.Append(xmlLoteRps);
            xmlLote.Append("</DPS>");

            retornoWebservice.XmlEnvio = xmlLote.ToString();
        }

        protected override XElement WriteRps(NotaServico nota)
        {
            var inscricaoFederal = nota.Prestador.CpfCnpj.IsCPF() ? "1" : "2";
            var numeroDps = $"{nota.Servico.CodigoMunicipio}{inscricaoFederal}{nota.Prestador.CpfCnpj.OnlyNumbers().PadLeft(14, '0')}{nota.IdentificacaoRps.Serie.PadLeft(5, '0')}{nota.IdentificacaoRps.Numero.PadLeft(15, '0')}";
            var rps = new XElement("infDPS", new XAttribute("Id", $"DPS{numeroDps}"));
            rps.AddChild(AdicionarTag(TipoCampo.Int, "", "tpAmb", 1, 1, Ocorrencia.Obrigatoria,
                Configuracoes.WebServices.Ambiente == DFeTipoAmbiente.Homologacao ? 2 : 1));
            rps.AddChild(AdicionarTag(TipoCampo.DatHorTz, "", "dhEmi", 20, 20, Ocorrencia.Obrigatoria,
                nota.IdentificacaoRps.DataEmissao));
            rps.AddChild(AdicionarTag(TipoCampo.Str, "", "verAplic", 1, 1, Ocorrencia.Obrigatoria, "1.0.0"));
            rps.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "serie", 1, 5, Ocorrencia.Obrigatoria, nota.IdentificacaoRps.Serie.PadLeft(5, '0')));
            rps.AddChild(AdicionarTag(TipoCampo.Int, "", "nDPS", 1, 1, Ocorrencia.Obrigatoria, nota.IdentificacaoRps.Numero));
            rps.AddChild(AdicionarTag(TipoCampo.Dat, "", "dCompet", 10, 10, Ocorrencia.Obrigatoria,
                nota.IdentificacaoRps.DataEmissao));
            rps.AddChild(AdicionarTag(TipoCampo.Int, "", "tpEmit", 1, 1, Ocorrencia.Obrigatoria, 1));
            rps.AddChild(AdicionarTag(TipoCampo.Int, "", "cLocEmi", 1, 1, Ocorrencia.Obrigatoria, nota.Servico.CodigoMunicipio));
            rps.AddChild(WritePrestadorRps(nota));
            rps.AddChild(WriteTomadorRps(nota));
            rps.AddChild(WriteServico(nota));
            rps.AddChild(WriteServicosValoresRps(nota));
            rps.AddChild(WriteIBSCBS(nota));
            return rps;
        }

        protected override XElement WritePrestadorRps(NotaServico nota)
        {
            var prestador = new XElement("prest");
            prestador.AddChild(AdicionarTagCNPJCPF("", "CPF", "CNPJ", nota.Prestador.CpfCnpj));
            prestador.AddChild(AdicionarTag(TipoCampo.Str, "", "email", 1, 80, Ocorrencia.NaoObrigatoria, nota.Prestador.DadosContato?.Email));

            string regimeEspecialTributacao;
            string optanteSimplesNacional;
            string regimeApuracaoTributosSimplesNacional = null;
            if (nota.RegimeEspecialTributacao == RegimeEspecialTributacao.SimplesNacional)
            {
                regimeEspecialTributacao = "0";
                regimeApuracaoTributosSimplesNacional = "1";
                optanteSimplesNacional = "3";
            }
            else
            {
                var regime = (int)nota.RegimeEspecialTributacao;
                regimeEspecialTributacao = regime == 0 ? "0" : "9";
                optanteSimplesNacional = "1";
            }
            
            var regimeTributario = new XElement("regTrib");
            regimeTributario.AddChild(AdicionarTag(TipoCampo.Int, "", "opSimpNac", 1, 1, Ocorrencia.Obrigatoria, optanteSimplesNacional));
            regimeTributario.AddChild(AdicionarTag(TipoCampo.Int, "", "regApTribSN", 1, 1, Ocorrencia.NaoObrigatoria, regimeApuracaoTributosSimplesNacional));
            regimeTributario.AddChild(AdicionarTag(TipoCampo.Int, "", "regEspTrib", 1, 1, Ocorrencia.Obrigatoria, regimeEspecialTributacao));
            prestador.Add(regimeTributario);

            return prestador;
        }

        protected override XElement WriteTomadorRps(NotaServico nota)
        {
            var tomador = new XElement("toma");

            tomador.AddChild(AdicionarTagCNPJCPF("", "CPF", "CNPJ", nota.Tomador.CpfCnpj));
            tomador.AddChild(AdicionarTag(TipoCampo.Str, "", "xNome", 1, 115, Ocorrencia.NaoObrigatoria,
                nota.Tomador.RazaoSocial));

            if (!nota.Tomador.Endereco.Logradouro.IsEmpty() || !nota.Tomador.Endereco.Numero.IsEmpty() ||
                !nota.Tomador.Endereco.Complemento.IsEmpty() || !nota.Tomador.Endereco.Bairro.IsEmpty() ||
                nota.Tomador.Endereco.CodigoMunicipio > 0 || !nota.Tomador.Endereco.Uf.IsEmpty() ||
                !nota.Tomador.Endereco.Cep.IsEmpty())
            {
                var endereco = new XElement("end");
                tomador.Add(endereco);

                var nacional = new XElement("endNac");
                nacional.AddChild(AdicionarTag(TipoCampo.Int, "", "cMun", 7, 7, Ocorrencia.MaiorQueZero,
                    nota.Tomador.Endereco.CodigoMunicipio));
                nacional.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "CEP", 8, 8, Ocorrencia.NaoObrigatoria,
                    nota.Tomador.Endereco.Cep));
                endereco.Add(nacional);

                endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "xLgr", 1, 125, Ocorrencia.NaoObrigatoria,
                    nota.Tomador.Endereco.Logradouro));
                endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "nro", 1, 10, Ocorrencia.NaoObrigatoria,
                    nota.Tomador.Endereco.Numero));
                endereco.AddChild(AdicionarTag(TipoCampo.Str, "", "xBairro", 1, 60, Ocorrencia.NaoObrigatoria,
                    nota.Tomador.Endereco.Bairro));
            }

            return tomador;
        }

        protected XElement WriteServico(NotaServico nota)
        {
            var servico = new XElement("serv");

            var localPrestacao = new XElement("locPrest");
            localPrestacao.AddChild(AdicionarTag(TipoCampo.Int, "", "cLocPrestacao", 7, 7, Ocorrencia.MaiorQueZero, nota.Servico.CodigoMunicipio));
            servico.Add(localPrestacao);

            var complemento = new XElement("cServ");
            complemento.AddChild(AdicionarTag(TipoCampo.Int, "", "cTribNac", 6, 6, Ocorrencia.MaiorQueZero, nota.Servico.CodigoTributacaoNacional));
            complemento.AddChild(AdicionarTag(TipoCampo.Int, "", "cTribMun", 3, 3, Ocorrencia.NaoObrigatoria, nota.Servico.CodigoTributacaoMunicipio));
            complemento.AddChild(AdicionarTag(TipoCampo.Str, "", "xDescServ", 1, 125, Ocorrencia.NaoObrigatoria, nota.Servico.Discriminacao));
            complemento.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "cNBS", 9, 9, Ocorrencia.NaoObrigatoria, nota.Servico.CodigoNbs));
            servico.Add(complemento);

            return servico;
        }

        protected override XElement WriteServicosValoresRps(NotaServico nota)
        {
            var valores = new XElement("valores");

            var servico = new XElement("vServPrest");
            valores.AddChild(servico);
            servico.AddChild(AdicionarTag(TipoCampo.De2, "", "vServ", 1, 15, Ocorrencia.Obrigatoria, nota.Servico.Valores.ValorServicos));

            var trib = new XElement("trib");
            
            var tribMun = new XElement("tribMun");
            // nota.Servico.ExigibilidadeIss = (ExigibilidadeIss)(rootServico.ElementAnyNs("ExigibilidadeISS")?.GetValue<int>() - 1 ?? 0);
            int valorISSQN;
            switch (nota.Servico.ExigibilidadeIss)
            {
                case ExigibilidadeIss.Exigivel:
                    valorISSQN = 1;
                    break;
                case ExigibilidadeIss.Isencao:
                    valorISSQN = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            tribMun.AddChild(AdicionarTag(TipoCampo.Int, "", "tribISSQN", 1, 1, Ocorrencia.Obrigatoria, valorISSQN));
            tribMun.AddChild(AdicionarTag(TipoCampo.Int, "", "tpRetISSQN", 1, 1, Ocorrencia.Obrigatoria, 1));
            trib.AddChild(tribMun);

            if (nota.RegimeEspecialTributacao == RegimeEspecialTributacao.SimplesNacional)
            {
                var tribFed = new XElement("tribFed");
                var piscofins = new XElement("piscofins");
                tribFed.AddChild(piscofins);
                piscofins.AddChild(AdicionarTag(TipoCampo.Int, "", "CST", 2, 2, Ocorrencia.Obrigatoria, "00"));
                piscofins.AddChild(AdicionarTag(TipoCampo.Int, "", "tpRetPisCofins", 1, 1, Ocorrencia.Obrigatoria, "0"));
                trib.AddChild(tribFed);
            }

            var totTrib = new XElement("totTrib");

            if (nota.Servico.Valores.EstimativaTributoPercentualFederal != 0 
                || nota.Servico.Valores.EstimativaTributoPercentualEstadual != 0 
                || nota.Servico.Valores.EstimativaTributoPercentualMunicipal != 0)
            {
                var vTotTrib = new XElement("pTotTrib");
                vTotTrib.AddChild(AdicionarTag(TipoCampo.De2, "", "pTotTribFed", 1, 1, Ocorrencia.Obrigatoria, nota.Servico.Valores.EstimativaTributoPercentualFederal));
                vTotTrib.AddChild(AdicionarTag(TipoCampo.De2, "", "pTotTribEst", 1, 1, Ocorrencia.Obrigatoria, nota.Servico.Valores.EstimativaTributoPercentualEstadual));
                vTotTrib.AddChild(AdicionarTag(TipoCampo.De2, "", "pTotTribMun", 1, 1, Ocorrencia.Obrigatoria, nota.Servico.Valores.EstimativaTributoPercentualMunicipal));
                totTrib.AddChild(vTotTrib);
            }
            else
            {
                totTrib.AddChild(AdicionarTag(TipoCampo.Int, "", "indTotTrib", 1, 1, Ocorrencia.Obrigatoria, 0));
            }

            // totTrib.AddChild(AdicionarTag(TipoCampo.Int, "", "pTotTribSN", 1, 1, Ocorrencia.Obrigatoria, 0));
            trib.AddChild(totTrib);

            // trib.AddChild(AdicionarTag(TipoCampo.Str, "", "CodigoTributacaoMunicipio", 1, 20, Ocorrencia.NaoObrigatoria, nota.Servico.CodigoTributacaoMunicipio));
            // servico.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "CodigoMunicipio", 1, 7, Ocorrencia.Obrigatoria, nota.Servico.CodigoMunicipio));
            // servico.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "CodigoMunicipio", 1, 7, Ocorrencia.Obrigatoria, nota.Servico.CodigoMunicipio));
            // xmlLote.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            valores.AddChild(trib);
            return valores;
        }
        
        protected XElement WriteIBSCBS(NotaServico nota)
        {
            var IBSCBS = new XElement("IBSCBS");
            
            IBSCBS.AddChild(AdicionarTag(TipoCampo.Int, "", "finNFSe", 0, 1, Ocorrencia.Obrigatoria, 0));
            IBSCBS.AddChild(AdicionarTag(TipoCampo.Int, "", "indFinal", 0, 1, Ocorrencia.Obrigatoria, 0));
            IBSCBS.AddChild(AdicionarTag(TipoCampo.Int, "", "cIndOp", 0, 6, Ocorrencia.Obrigatoria, nota.Servico.CodigoIhdicadorOperacaoFornecimento));
            IBSCBS.AddChild(AdicionarTag(TipoCampo.Int, "", "indDest", 0, 1, Ocorrencia.Obrigatoria, 0));

            var valores = new XElement("valores");
            IBSCBS.AddChild(valores);
            var trib = new XElement("trib");
            valores.AddChild(trib);
            var gib = new XElement("gIBSCBS");
            trib.AddChild(gib);
            
            gib.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "CST", 1, 3, Ocorrencia.Obrigatoria, "000".PadLeft(3, '0')));
            gib.AddChild(AdicionarTag(TipoCampo.StrNumber, "", "cClassTrib", 1, 6, Ocorrencia.Obrigatoria, "1".PadLeft(6, '0')));

            return IBSCBS;
        }

        protected override void TratarRetornoEnviar(RetornoEnviar retornoWebservice, NotaServicoCollection notas)
        {
            if (string.IsNullOrWhiteSpace(retornoWebservice.XmlRetorno))
            {
                retornoWebservice.Sucesso = false;
                retornoWebservice.Erros.Add(new Evento
                {
                    Descricao = $"Forçando a falha... analisar o retorno manualmente\n{retornoWebservice.EnvelopeRetorno}",
                });
            }
            else
            {
                retornoWebservice.Sucesso = true;
            }
        }
        
        private static bool ValidarXml(
            string arquivoXml,
            string[] schema,
            out string[] erros,
            out string[] avisos)
        {
            List<string> errorList = new List<string>();
            List<string> avisosList = new List<string>();
            if (string.IsNullOrEmpty(arquivoXml))
            {
                errorList.Add("Arquivo Xml não encontrado.");
                erros = errorList.ToArray();
                avisos = avisosList.ToArray();
                return false;
            }

            foreach (var s in schema)
            {
                if (!File.Exists(s))
                {
                    errorList.Add("Arquivo de Schema não encontrado.");
                    erros = errorList.ToArray();
                    avisos = avisosList.ToArray();
                    return false;
                }
            }

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings()
                {
                    ValidationType = ValidationType.Schema,
                    Schemas =
                    {
                        XmlResolver = (XmlResolver)new XmlUrlResolver()
                    }
                };
                foreach (var s in schema)
                {
                    XmlSchema schema1 = XmlSchema.Read((XmlReader)new XmlTextReader(s),
                        (ValidationEventHandler)((sender, args) =>
                        {
                            switch (args.Severity)
                            {
                                case XmlSeverityType.Error:
                                    errorList.Add(args.Message);
                                    break;
                                case XmlSeverityType.Warning:
                                    avisosList.Add(args.Message);
                                    break;
                            }

                            if (args.Exception == null)
                                return;
                            List<string> stringList = errorList;
                            string[] strArray = new string[8];
                            strArray[0] = "\nErro: ";
                            strArray[1] = args.Exception.Message;
                            strArray[2] = "\nLinha ";
                            int num = args.Exception.LinePosition;
                            strArray[3] = num.ToString();
                            strArray[4] = " - Coluna ";
                            num = args.Exception.LineNumber;
                            strArray[5] = num.ToString();
                            strArray[6] = "\nSource: ";
                            strArray[7] = args.Exception.SourceUri;
                            string str = string.Concat(strArray);
                            stringList.Add(str);
                        }));
                    settings.Schemas.Add(schema1);
                }
                
                using (XmlReader xmlReader = XmlReader.Create((TextReader)new StringReader(arquivoXml), settings))
                {
                    do
                        ;
                    while (xmlReader.Read());
                }
            }
            catch (Exception ex)
            {
                errorList.Add(ex.Message);
            }

            erros = errorList.ToArray();
            avisos = avisosList.ToArray();
            errorList = (List<string>)null;
            avisosList = (List<string>)null;
            return erros.Length < 1;
        }

        #endregion Protected Methods

        #endregion Methods
    }
}