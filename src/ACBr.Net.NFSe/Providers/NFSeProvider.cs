// ***********************************************************************
// Assembly         : ACBr.Net.NFSe
// Author           : Rafael Dias
// Created          : 07-30-2017
//
// Last Modified By : Rafael Dias
// Last Modified On : 07-30-2017
// ***********************************************************************
// <copyright file="NFSeProvider.cs" company="ACBr.Net">
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

using System.ComponentModel;

namespace ACBr.Net.NFSe.Providers
{
    public enum NFSeProvider : byte
    {
        Abaco = 0,

        Betha = 1,

        [Description("Betha v2")]
        Betha2 = 2,

        BHISS = 8,

        Coplan = 3,

        DBSeller = 19,

        DSF = 4,

        Equiplano = 15,

        Fiorilli = 16,

        FissLex = 12,

        Ginfes = 5,

        ISSNet = 18,

        [Description("NFe Cidades")]
        NFeCidades = 6,

        [Description("Nota Carioca")]
        NotaCarioca = 7,

        [Description("Pronim v2")]
        Pronim2 = 17,

        [Description("S�o Paulo")]
        SaoPaulo = 9,

        [Description("SmarAPD ABRASF")]
        SmarAPDABRASF = 14,

        [Description("Vitoria")]
        Vitoria = 13,

        WebIss = 10,

        [Description("WebIss v2")]
        WebIss2 = 11,

        Sigiss = 20,

        [Description("CONAM")]
        Conam = 21,

        [Description("Maring�")]
        Maringa = 22,
    }
}