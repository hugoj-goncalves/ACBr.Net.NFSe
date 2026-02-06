// ***********************************************************************
// Assembly         : ACBr.Net.NFSe
// Author           : Rafael Dias
// Created          : 05-16-2018
//
// Last Modified By : Rafael Dias
// Last Modified On : 07-11-2018
// ***********************************************************************
// <copyright file="ProviderISSe.cs" company="ACBr.Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2016 Grupo ACBr.Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ACBr.Net.Core;
using ACBr.Net.Core.Extensions;
using ACBr.Net.DFe.Core;
using ACBr.Net.DFe.Core.Serializer;
using ACBr.Net.NFSe.Configuracao;
using ACBr.Net.NFSe.Nota;

namespace ACBr.Net.NFSe.Providers
{
    internal sealed class ProviderPublica : ProviderABRASF
    {
        #region Constructors

        public ProviderPublica(ConfigNFSe config, ACBrMunicipioNFSe municipio) : base(config, municipio)
        {
            Name = "Publica";
            // Versao = "1.00";
        }

        #endregion Constructors

        #region Methods

        #region Protected Methods

        protected override void AssinarEnviar(RetornoEnviar retornoWebservice)
        {
            // retornoWebservice.XmlEnvio = XmlSigning.AssinarXmlTodos(retornoWebservice.XmlEnvio, "Rps", "InfRps", "id", Certificado);
            retornoWebservice.XmlEnvio = XmlSigning.AssinarXml(retornoWebservice.XmlEnvio, "Rps", "InfRps", "id", Certificado);
            
            // retornoWebservice.XmlEnvio = XmlSigning.AssinarXml(retornoWebservice.XmlEnvio, "EnviarLoteRpsEnvio", "LoteRps", "Id", Certificado);
        }

        protected override void AssinarEnviarSincrono(RetornoEnviar retornoWebservice)
        {
            retornoWebservice.XmlEnvio = XmlSigning.AssinarXmlTodos(retornoWebservice.XmlEnvio, "Rps", "InfRps", "id", Certificado, true);
        }

        protected override IServiceClient GetClient(TipoUrl tipo)
        {
            return new PublicaServiceClient(this, tipo);
        }

        protected override string GetNamespace()
        {
            return
                "xmlns=\"http://www.publica.inf.br\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.publica.inf.br schema_nfse_v03.xsd\"";
        }

        protected override string GetSchema(TipoUrl tipo)
        {
            return "schema_nfse_v03.xsd";
        }

        protected override bool PrecisaValidarSchema(TipoUrl tipo)
        {
            return false;
        }

        protected override XElement WriteInfoRPS(NotaServico nota)
        {
            var incentivadorCultural = nota.IncentivadorCultural == NFSeSimNao.Sim ? 1 : 2;

            string regimeEspecialTributacao;
            string optanteSimplesNacional;
            if (nota.RegimeEspecialTributacao == RegimeEspecialTributacao.SimplesNacional)
            {
                regimeEspecialTributacao = "6";
                optanteSimplesNacional = "1";
            }
            else
            {
                var regime = (int)nota.RegimeEspecialTributacao;
                regimeEspecialTributacao = regime == 0 ? string.Empty : regime.ToString();
                optanteSimplesNacional = "2";
            }

            var situacao = nota.Situacao == SituacaoNFSeRps.Normal ? "1" : "2";

            var infoRps = new XElement("InfRps", new XAttribute("id", $"R{nota.IdentificacaoRps.Numero}"));

            infoRps.Add(WriteIdentificacao(nota));
            infoRps.AddChild(AdicionarTag(TipoCampo.DatHor, "", "DataEmissao", 20, 20, Ocorrencia.Obrigatoria, nota.IdentificacaoRps.DataEmissao));
            infoRps.AddChild(AdicionarTag(TipoCampo.Int, "", "NaturezaOperacao", 1, 1, Ocorrencia.Obrigatoria, nota.NaturezaOperacao));
            // infoRps.AddChild(AdicionarTag(TipoCampo.Int, "", "RegimeEspecialTributacao", 1, 1, Ocorrencia.NaoObrigatoria, regimeEspecialTributacao));
            infoRps.AddChild(AdicionarTag(TipoCampo.Int, "", "OptanteSimplesNacional", 1, 1, Ocorrencia.Obrigatoria, optanteSimplesNacional));
            infoRps.AddChild(AdicionarTag(TipoCampo.Int, "", "IncentivadorCultural", 1, 1, Ocorrencia.Obrigatoria, incentivadorCultural));
            infoRps.AddChild(AdicionarTag(TipoCampo.Int, "", "Status", 1, 1, Ocorrencia.Obrigatoria, situacao));

            return infoRps;
        }

        protected override void PrepararEnviar(RetornoEnviar retornoWebservice, NotaServicoCollection notas)
        {
            if (retornoWebservice.Lote == 0) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "Lote n�o informado." });
            if (notas.Count == 0) retornoWebservice.Erros.Add(new Evento { Codigo = "0", Descricao = "RPS n�o informado." });
            if (retornoWebservice.Erros.Count > 0) return;

            var xmlLoteRps = new StringBuilder();

            foreach (var nota in notas)
            {
                var xmlRps = WriteXmlRps(nota, false, false);
                xmlLoteRps.Append(xmlRps);
                GravarRpsEmDisco(xmlRps, $"Rps-{nota.IdentificacaoRps.DataEmissao:yyyyMMdd}-{nota.IdentificacaoRps.Numero}.xml", nota.IdentificacaoRps.DataEmissao);
            }

            var xmlLote = new StringBuilder();
            xmlLote.Append($"<EnviarLoteRpsEnvio {GetNamespace()}>");
            xmlLote.Append($"<LoteRps versao=\"1.00\">");
            xmlLote.Append($"<NumeroLote>{retornoWebservice.Lote}</NumeroLote>");
            xmlLote.Append($"<Cnpj>{Configuracoes.PrestadorPadrao.CpfCnpj.ZeroFill(14)}</Cnpj>");
            xmlLote.Append($"<InscricaoMunicipal>{Configuracoes.PrestadorPadrao.InscricaoMunicipal}</InscricaoMunicipal>");
            xmlLote.Append($"<QuantidadeRps>{notas.Count}</QuantidadeRps>");
            xmlLote.Append("<ListaRps>");
            xmlLote.Append(xmlLoteRps);
            xmlLote.Append("</ListaRps>");
            xmlLote.Append("</LoteRps>");
            xmlLote.Append("</EnviarLoteRpsEnvio>");

            retornoWebservice.XmlEnvio = xmlLote.ToString();
        }

        #endregion Protected Methods

        #endregion Methods
        
        
        
        private static string AssinarXmlTodos(
            string xml,
            string docElement,
            string infoElement,
            string signAtribute,
            X509Certificate2 certificado,
            bool comments = false,
            bool identado = false,
            bool showDeclaration = true,
            SignDigest digest = SignDigest.SHA1)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                XmlElement[] array;
                if (infoElement.IsEmpty())
                {
                    array = xmlDoc.GetElementsByTagName(docElement).Cast<XmlElement>().ToArray<XmlElement>();
                }
                else
                {
                    array = xmlDoc.GetElementsByTagName(docElement).Cast<XmlElement>().Where<XmlElement>((Func<XmlElement, bool>) (x => x.GetElementsByTagName(infoElement).Count == 1)).ToArray<XmlElement>();
                    Guard.Against<ACBrDFeException>(!((IEnumerable<XmlElement>) array).Any<XmlElement>(), "Nome do elemento de assinatura incorreto", (Action<ACBrDFeException>) null);
                }
                foreach (XmlElement oldChild in array)
                {
                    XmlDocument doc = new XmlDocument()
                    {
                        PreserveWhitespace = true
                    };
                    doc.LoadXml(oldChild.OuterXml);
                    AssinarDocumento(doc, docElement, infoElement, signAtribute, certificado, comments, digest);
                    XmlNode newChild = xmlDoc.ImportNode((XmlNode) doc.DocumentElement, true);
                    oldChild.ParentNode?.ReplaceChild(newChild, (XmlNode) oldChild);
                }
                return xmlDoc.AsString(identado, showDeclaration);
            }
            catch (Exception ex)
            {
                throw new ACBrDFeException("Erro ao efetuar assinatura digital.", ex);
            }
        }
        
        private static void AssinarDocumento(
            XmlDocument doc,
            string docElement,
            string infoElement,
            string signAtribute,
            X509Certificate2 certificado,
            bool comments = false,
            SignDigest digest = SignDigest.SHA1)
        {
            Guard.Against<ArgumentNullException>(doc == null, "XmlDOcument não pode ser nulo.", (Action<ArgumentNullException>) null);
            Guard.Against<ArgumentException>(docElement.IsEmpty(), "docElement não pode ser nulo ou vazio.", (Action<ArgumentException>) null);
            XmlElement node = GerarAssinatura(doc, infoElement, signAtribute, certificado, comments, digest);
            XmlElement xmlElement = doc.GetElementsByTagName(docElement).Cast<XmlElement>().FirstOrDefault<XmlElement>();
            Guard.Against<ACBrDFeException>(xmlElement == null, "Elemento principal não encontrado.", (Action<ACBrDFeException>) null);
            xmlElement.AppendChild(doc.ImportNode((XmlNode) node, true));
        }

        private static XmlElement GerarAssinatura(
            XmlDocument doc,
            string infoElement,
            string signAtribute,
            X509Certificate2 certificado,
            bool comments,
            SignDigest digest)
        {
            Guard.Against<ArgumentException>(!infoElement.IsEmpty() && doc.GetElementsByTagName(infoElement).Count != 1, "Referencia invalida ou não é unica.", (Action<ArgumentException>) null);
            System.Security.Cryptography.Xml.KeyInfo keyInfo = new System.Security.Cryptography.Xml.KeyInfo();
            keyInfo.AddClause((KeyInfoClause) new KeyInfoX509Data((X509Certificate) certificado));
            keyInfo.AddClause(new RSAKeyValue((RSA)certificado.PrivateKey)); // RSAKeyValue não é recomendado pela doc...
            SignedXml signedXml = new SignedXml(doc);
            signedXml.SigningKey = certificado.PrivateKey;
            signedXml.KeyInfo = keyInfo;
            signedXml.SignedInfo.SignatureMethod = GetSignatureMethod(digest);
            string str = infoElement.IsEmpty() || signAtribute.IsEmpty() ? "" : "#" + doc.GetElementsByTagName(infoElement)[0].Attributes?[signAtribute]?.InnerText;
            System.Security.Cryptography.Xml.Reference reference = new System.Security.Cryptography.Xml.Reference()
            {
                Uri = str,
                DigestMethod = GetDigestMethod(digest)
            };
            reference.AddTransform((System.Security.Cryptography.Xml.Transform) new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform((System.Security.Cryptography.Xml.Transform) new XmlDsigC14NTransform(comments));
            var xpathTransform = new XmlDsigXPathTransform();
            var xpathDoc = new XmlDocument();
            xpathDoc.LoadXml(
                "<XPath xmlns:ds='http://www.w3.org/2000/09/xmldsig#'>" +
                "not(ancestor-or-self::ds:Signature)" +
                "</XPath>"
            );
            xpathTransform.LoadInnerXml(xpathDoc.ChildNodes);
            reference.AddTransform(xpathTransform);
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();
            
            var signatureElement = signedXml.GetXml();
            signatureElement.Prefix = "ds";
            SetPrefixRecursive(signatureElement, "ds");
            signedXml.LoadXml(signatureElement);
            signedXml.SignedInfo.References.Clear();
            signedXml.ComputeSignature();
            
            var recomputedSignature = Convert.ToBase64String(signedXml.SignatureValue);
            ReplaceSignature(signatureElement, recomputedSignature);
            
            return signatureElement;
        }
        
        private static void SetPrefixRecursive(XmlNode node, string prefix)
        {
            if (node is XmlElement el && el.NamespaceURI == SignedXml.XmlDsigNamespaceUrl)
            {
                el.Prefix = prefix;
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                SetPrefixRecursive(child, prefix);
            }
        }
        
        private static string GetSignatureMethod(SignDigest digest)
        {
            if (digest == SignDigest.SHA1)
                return "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
            if (digest == SignDigest.SHA256)
                return "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            throw new ArgumentOutOfRangeException(nameof (digest), (object) digest, (string) null);
        }

        private static string GetDigestMethod(SignDigest digest)
        {
            if (digest == SignDigest.SHA1)
                return "http://www.w3.org/2000/09/xmldsig#sha1";
            if (digest == SignDigest.SHA256)
                return "http://www.w3.org/2001/04/xmlenc#sha256";
            throw new ArgumentOutOfRangeException(nameof (digest), (object) digest, (string) null);
        }
        
        private static void SetPrefix(string prefix, XmlNode node)
        {
            node.Prefix = prefix;
            foreach (XmlNode n in node.ChildNodes)
            {
                SetPrefix(prefix, n);
            }
        }

        private static void ReplaceSignature(XmlElement signature, string newValue)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));
            if (signature.OwnerDocument == null) throw new ArgumentException("No owner document", nameof(signature));

            XmlNamespaceManager nsm = new XmlNamespaceManager(signature.OwnerDocument.NameTable);
            nsm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            XmlNode signatureValue = signature.SelectSingleNode("ds:SignatureValue", nsm);
            if (signatureValue == null)
                throw new Exception("Signature does not contain 'ds:SignatureValue'");

            signatureValue.InnerXml = newValue;
        }
    }
}
