// ***********************************************************************
// Assembly         : ACBr.Net.NFSe
// Author           : Rafael Dias
// Created          : 01-31-2016
//
// Last Modified By : Rafael Dias
// Last Modified On : 06-07-2016
// ***********************************************************************
// <copyright file="ConfigArquivosNFSe.cs" company="ACBr.Net">
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

using ACBr.Net.DFe.Core.Common;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using ACBr.Net.Core;
using ACBr.Net.Core.Extensions;
using ACBr.Net.NFSe.Providers;

namespace ACBr.Net.NFSe.Configuracao
{
    [TypeConverter(typeof(ACBrExpandableObjectConverter))]
    public sealed class ConfigArquivosNFSe : DFeArquivosConfigBase<ACBrNFSe>
    {
        #region Constructor

        /// <summary>
        /// Inicializa uma nova instancia da classe <see cref="ConfigArquivosNFSe"/>.
        /// </summary>
        internal ConfigArquivosNFSe(ACBrNFSe parent) : base(parent)
        {
            EmissaoPathNFSe = false;

            var path = Assembly.GetExecutingAssembly().GetPath();
            path = Path.Combine(path, "..", "logs-nf");
            if (!path.IsEmpty())
            {
                PathNFSe = Path.Combine(path, "NFSe");
                PathLote = Path.Combine(path, "Lote");
                PathRps = Path.Combine(path, "RPS");
            }
            else
            {
                PathNFSe = string.Empty;
                PathLote = string.Empty;
                PathRps = string.Empty;
            }
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether [emissao path n fe].
        /// </summary>
        /// <value><c>true</c> if [emissao path n fe]; otherwise, <c>false</c>.</value>
        [Browsable(true)]
        [DefaultValue(false)]
        public bool EmissaoPathNFSe { get; set; }

        /// <summary>
        /// Gets or sets the path n fe.
        /// </summary>
        /// <value>The path n fe.</value>
        [Browsable(true)]
        public string PathNFSe { get; set; }

        /// <summary>
        /// Gets or sets the path lote.
        /// </summary>
        /// <value>The path lote.</value>
        [Browsable(true)]
        public string PathLote { get; set; }

        /// <summary>
        /// Gets or sets the path lote.
        /// </summary>
        /// <value>The path lote.</value>
        [Browsable(true)]
        public string PathRps { get; set; }

        #endregion Properties

        #region Methods

        public string GetPathSoap(DateTime data, string cnpj = "")
        {
            return GetPath(PathNFSe, "SOAP", cnpj, data);
        }

        public string GetPathNFSe(DateTime data, string cnpj = "")
        {
            return GetPath(PathNFSe, "NFSe", cnpj, data, "NFSe");
        }

        public string GetPathLote(DateTime data, string cnpj = "")
        {
            return GetPath(PathLote, "Lote", cnpj, data);
        }

        public string GetPathRps(DateTime data, string cnpj = "")
        {
            return GetPath(PathRps, "Rps", cnpj, data, "Rps");
        }

        /// <inheritdoc />
        protected override void ArquivoServicoChange()
        {
            ProviderManager.Load(ArquivoServicos);
        }

        #endregion Methods
    }
}