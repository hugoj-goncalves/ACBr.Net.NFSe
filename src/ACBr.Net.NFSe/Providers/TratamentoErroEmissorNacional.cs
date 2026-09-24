using Newtonsoft.Json;

namespace ACBr.Net.NFSe.Providers
{
    // {
    //     "tipoAmbiente" : 2,
    //     "versaoAplicativo" : "SefinNacional_1.6.0",
    //     "dataHoraProcessamento" : "2026-09-24T12:57:38.2323009-03:00",
    //     "idDPS" : "DPS420200826401564000015800001000000000000006",
    //     "erros" : [ {
    //         "Codigo" : "E0240",
    //         "Descricao" : "O CEP informado para o endereço nacional do tomador do serviço não existe ou não pertence ao município do endereço do tomador."
    //     } ]
    // }
    public class TratamentoErroEmissorNacional
    {
        [JsonProperty("tipoAmbiente")]
        public int TipoAmbiente { get; set; }   
        
        [JsonProperty("versaoAplicativo")]
        public string VersaoAplicativo { get; set; }   
        
        [JsonProperty("idDPS")]
        public string IdDPS { get; set; }   
        
        [JsonProperty("erros")]
        public TratamentoErroEmissorNacionalErro[] Erros { get; set; }   
    }
    
    public class TratamentoErroEmissorNacionalErro
    {
        [JsonProperty("Codigo")]
        public string Codigo { get; set; }   
        
        [JsonProperty("Descricao")]
        public string Descricao { get; set; }   
    }
}