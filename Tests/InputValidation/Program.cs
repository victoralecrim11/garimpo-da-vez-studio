using GarimpoDaVezStudio.Services;

var cases = new (string label, bool actual, bool expected)[]
{
    ("anúncio HTTPS válido", ProductInputValidator.IsMercadoLivreListing("https://www.mercadolivre.com.br/anuncio-MLB123456"), true),
    ("link curto não é URL de anúncio original", ProductInputValidator.IsMercadoLivreListing("https://meli.la/ABCDE123"), false),
    ("link de afiliado curto válido", ProductInputValidator.IsMercadoLivreAffiliate("https://meli.la/2Ea7BrK"), true),
    ("link de afiliado de domínio externo", ProductInputValidator.IsMercadoLivreAffiliate("https://dominio-invalido.com/item"), false),
    ("link externo fingindo ser Mercado Livre", ProductInputValidator.IsMercadoLivreListing("https://mercadolivre.com.br.exemplo.com/"), false),
    ("HTTP proibido", ProductInputValidator.IsMercadoLivreListing("http://mercadolivre.com.br/item"), false),
    ("URL com usuário e senha", ProductInputValidator.IsValidHttpsUrl("https://usuario:senha@www.mercadolivre.com.br/"), false),
    ("porta fora de HTTPS padrão", ProductInputValidator.IsValidHttpsUrl("https://www.mercadolivre.com.br:8443/item"), false),
    ("ID MLB alfanumérico permitido", ProductInputValidator.IsValidExternalId("MLB1234567890"), true),
    ("SKU com hífen permitido", ProductInputValidator.IsValidExternalId("RS1033-VHDC"), true),
    ("ID com espaços proibido", ProductInputValidator.IsValidExternalId("MLB 12345"), false),
    ("App ID numérico permitido", ProductInputValidator.IsValidClientId("40953651847738888"), true),
    ("App ID alfanumérico proibido", ProductInputValidator.IsValidClientId("abc12345"), false),
    ("nome de produto texto com números", ProductInputValidator.HasValidContent("Varal 3 andares", 180, required:true), true),
    ("nome obrigatório vazio", ProductInputValidator.HasValidContent("  ", 180, required:true), false),
    ("URL opcional vazia", ProductInputValidator.IsMercadoLivreListing(""), true),
    ("URL obrigatória vazia", ProductInputValidator.IsMercadoLivreListing("", required: true), false),
};
var officialCases = new (string label, MercadoLivreListing listing, bool expected)[]
{
    ("permalink com ID correspondente", new MercadoLivreListing {
        Id = "MLB5227147039", Permalink =
            "https://produto.mercadolivre.com.br/MLB-5227147039-computador-portatil-hp-victus-_JM"
    }, true),
    ("ID diferente do URL", new MercadoLivreListing {
        Id = "MLB5227147039", Permalink =
            "https://produto.mercadolivre.com.br/MLB-1111111111-outro-anuncio-_JM"
    }, false),
    ("referência de catálogo não é anúncio", new MercadoLivreListing {
        Id = "MLBU300001", Permalink = "https://www.mercadolivre.com.br/p/MLBU300001"
    }, false),
    ("URL sem ID oficial não vincula automaticamente", new MercadoLivreListing {
        Id = "MLB5227147039", Permalink = "https://www.mercadolivre.com.br/notebook-victus"
    }, false),
    ("domínio estrangeiro bloqueado", new MercadoLivreListing {
        Id = "MLB5227147039", Permalink =
            "https://produto.mercadolibre.cl/MLB-5227147039-produto-_JM"
    }, false)
};
int failures = 0;
foreach (var c in cases)
{
    if (c.actual == c.expected) continue;
    failures++;
    Console.Error.WriteLine($"FALHA: {c.label}: esperado {c.expected}, obtido {c.actual}");
}
foreach (var scenario in officialCases)
{
    var result = OfficialListingMatcher.TryGetVerifiedPermalink(scenario.listing, out var url);
    if (result == scenario.expected && (!result || url == scenario.listing.Permalink))
        continue;
    failures++;
    Console.Error.WriteLine("FALHA: validação da URL oficial: " + scenario.label);
}
if (failures > 0) return 1;
Console.WriteLine($"{cases.Length + officialCases.Length} casos de validação de entradas e URLs aprovados.");
return 0;
