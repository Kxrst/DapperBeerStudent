using System.Data;
using BenchmarkDotNet.Attributes;
using Dapper;
using DapperBeer.DTO;
using DapperBeer.Model;
using DapperBeer.Tests;

namespace DapperBeer;

public class Assignments3
{
    // 3.1 Question
    // Tip: Kijk in voorbeelden en sheets voor inspiratie.
    // Deze staan in de directory ExampleFromSheets/Relationships.cs. 
    // De sheets kan je vinden op: https://slides.com/jorislops/dapper/
    // Kijk niet te veel naar de voorbeelden van relaties op https://www.learndapper.com/relationships
    // Deze aanpak is niet altijd de manier de gewenst is!
    
    // 1 op 1 relatie (one-to-one relationship)
    // Een brouwmeester heeft altijd 1 adres. Haal alle brouwmeesters op en zorg ervoor dat het address gevuld is.
    // Sorteer op naam.
    // Met andere woorden een brouwmeester heeft altijd een adres (Property Address van type Address), zie de klasse Brewmaster.
    // Je kan dit doen door een JOIN te gebruiken.
    // Je zult de map functie in Query<Brewmaster, Address, Brewmaster>(sql, map: ...) moeten gebruiken om de Address property van Brewmaster te vullen.
    // Kijk in voorbeelden hoe je dit kan doen. Deze staan in de directory ExampleFromSheets/Relationships.cs.
    public static List<Brewmaster> GetAllBrouwmeestersIncludesAddress()
    {
        string sql = """
        Select 
        br.BrewmasterId AS BrewmasterId,
        br.Name As Name,
        '' As AddressSpilt,
        a.AddressId As AddressId,
        a.Street As Street,
        a.City As City,
        a.Country As Country
        From Brewmaster br
        Join address a 
            On br.addressId = a.AddressId
        ORDER BY br.Name 
        """;

        using var connection = DbHelper.GetConnection();
        var brewmaster = connection.Query<Brewmaster, Address, Brewmaster>(
                sql, 
                map: (brewmaster, address) =>
                {
                    brewmaster.Address = address; 
                    return brewmaster;
                },
                splitOn: "AddressSpilt")
            .ToList();
        return brewmaster;
    }

    // 3.2 Question
    // 1 op 1 relatie (one-to-one relationship)
    // Haal alle brouwmeesters op en zorg ervoor dat de brouwer (Brewer) gevuld is.
    // Sorteer op naam.
    public static List<Brewmaster> GetAllBrewmastersWithBrewery()
    {
        string sql = 
            """
                SELECT
                br.BrewmasterId AS BrewmasterId,
                br.Name As Name,
                '' As BrewerSpilt,       
                b.BrewerId As BrewerId,
                b.Name As Name,
                b.Country As Country
                From Brewmaster br
                JOIN Brewer
                    b on br.brewerId = b.BrewerId
                ORDER BY br.Name
            """;
        
        using var connection = DbHelper.GetConnection();
        var brewmaster = connection.Query<Brewmaster, Brewer, Brewmaster>(
            sql,
            map: (brewmaster, brewer) =>
            {
                brewmaster.Brewer = brewer;
                return brewmaster;
            },
            splitOn: "BrewerSpilt")
            .ToList();
        return brewmaster;
        
    }

    // 3.3 Question
    // 1 op 1 (0..1) (one-to-one relationship) 
    // Geef alle brouwers op en zorg ervoor dat de brouwmeester gevuld is.
    // Sorteer op brouwernaam.
    //
    // Niet alle brouwers hebben een brouwmeester.
    // Let op: gebruik het correcte type JOIN (JOIN, LEFT JOIN, RIGHT JOIN).
    // Dapper snapt niet dat het om een 1 - 0..1 relatie gaat.
    // De Query methode ziet er als volgt uit (let op het vraagteken optioneel):
    // Query<Brewer, Brewmaster?, Brewer>(sql, map: ...)
    // Wat je kan doen is in de map functie een controle toevoegen, je zou dit verwachten:
    // if (brewmaster is not null) { brewer.Brewmaster = brewmaster; }
    // !!echter dit werkt niet!!!!
    // Plaats eens een breakpoint en kijk wat er in de brewmaster variabele staat,
    // hoe moet dan je if worden?
    public static List<Brewer> GetAllBrewersIncludeBrewmaster()
    {
        string sql =
            """
                SELECT
                b.BrewerId AS BrewerId,
                b.Name As Name,
                b.Country As Country,
                '' As BrewmasterSpilt,
                br.BrewmasterId As BrewmasterId,
                br.Name As BrewmasterName
                From Brewer b
                LEFT JOIN Brewmaster br ON br.BrewerId = b.BrewerId
                ORDER BY b.Name
            """;
        using var connection = DbHelper.GetConnection();
        var brewer = connection.Query<Brewer, Brewmaster?, Brewer>(
                sql,
                map: (brewer, brewmaster ) =>
                {
                    if (brewmaster != null && brewmaster.BrewmasterId != 0)
                    {
                        brewer.Brewmaster = brewmaster;
                    }
                    return brewer;
                },
                splitOn: "BrewmasterSpilt")
            .ToList();
        return brewer;
    }
    
    // 3.4 Question
    // 1 op veel relatie (one-to-many relationship)
    // Geef een overzicht van alle bieren. Zorg ervoor dat de property Brewer gevuld is.
    // Sorteer op biernaam en beerId!!!!
    // Zorg ervoor dat bieren van dezelfde brouwerij naar dezelfde instantie van Brouwer verwijzen.
    // Dit kan je doen door een Dictionary<int, Brouwer> te gebruiken.
    // Kijk in voorbeelden hoe je dit kan doen. Deze staan in de directory ExampleFromSheets/Relationships.cs.
    public static List<Beer> GetAllBeersIncludeBrewery()
    {
        string sql =
            """
                SELECT
                b.BeerId AS BeerId,
                b.Name As Name,
                b.Type As Type,
                b.Style As Style,
                b.Alcohol As Alcohol,
                b.BrewerId As BrewerId,
                
                '' As BrewerSpilt,
                
                br.BrewerId As BrewerId,
                br.Name As Name,
                br.Country As Country
                From Beer b 
                    JOIN Brewer br ON b.BrewerId = br.BrewerId
                Order By b.Name, b.BeerId
            """;
        
        Dictionary<int, Brewer> brewerDictionary = new Dictionary<int, Brewer>();
        
        using var connection = DbHelper.GetConnection();
        List<Beer> beers = connection.Query<Beer, Brewer, Beer>(
            sql, 
            map: (beer, brewer) =>
            {
                if(brewerDictionary.ContainsKey(brewer.BrewerId)) 
                {
                    brewer = brewerDictionary[brewer.BrewerId];
                }
                else
                {
                    brewer = new Brewer
                    {
                        BrewerId = brewer.BrewerId,
                        Name = brewer.Name,
                        Country = brewer.Country,
                    };
                    brewerDictionary.Add(brewer.BrewerId, brewer);
                }
                
                beer.Brewer = brewer;
                return beer;
            },
            splitOn: "BrewerSpilt").ToList();
        return beers;
        
    }
    
    // 3.5 Question
    // N+1 probleem (1-to-many relationship)
    // Geef een overzicht van alle brouwerijen en hun bieren. Sorteer op brouwerijnaam en daarna op biernaam.
    // Doe dit door eerst een Query<Brewer>(...) te doen die alle brouwerijen ophaalt. (Dit is 1)
    // Loop (foreach) daarna door de brouwerijen en doe voor elke brouwerij een Query<Beer>(...)
    // die de bieren ophaalt voor die brouwerij. (Dit is N)
    // Dit is een N+1 probleem. Hoe los je dit op? Dat zien we in de volgende vragen.
    // Als N groot is (veel brouwerijen) dan kan dit een performance probleem zijn of worden. Probeer dit te voorkomen!
    public static List<Brewer> GetAllBrewersIncludingBeersNPlus1()
    {
        string sqlBrewers =
            """
                SELECT
                br.BrewerId AS BrewerId,
                br.Name As Name,
                br.Country As Country
                From Brewer br
                Order By br.Name
            """;
        
        using var connection = DbHelper.GetConnection();
        var brewers = connection.Query<Brewer>(sqlBrewers).ToList();

        foreach (var brewer in brewers)
        {
            string sqlBeers =
                """
                SELECT
                b.BeerId AS BeerId,
                b.Name As Name,
                b.Type As Type,
                b.Style As Style,
                b.Alcohol As Alcohol,
                b.BrewerId As BrewerId
                From Beer b 
                Where b.BrewerId = @BrewerId
                ORDER BY b.Name
                """;
            var beers = connection.Query<Beer>(sqlBeers, new {BrewerId = brewer.BrewerId}).ToList();
            brewer.Beers = beers;
        }
        return brewers;
        
    }
    
    // 3.6 Question
    // 1 op n relatie (one-to-many relationship)
    // Schrijf een query die een overzicht geeft van alle brouwerijen. Vul per brouwerij de property Beers (List<Beer>) met de bieren van die brouwerij.
    // Sorteer op brouwerijnaam en daarna op biernaam.
    // Gebruik de methode Query<Brewer, Beer, Brewer>(sql, map: ...)
    // Het is belangrijk dat je de map functie gebruikt om de bieren te vullen.
    // De query geeft per brouwerij meerdere bieren terug. Dit is een 1 op veel relatie.
    // Om ervoor te zorgen dat de bieren van dezelfde brouwerij naar dezelfde instantie van Brewer verwijzen,
    // moet je een Dictionary<int, Brewer> gebruiken.
    // Dit is een veel voorkomend patroon in Dapper.
    // Vergeet de Distinct() methode te gebruiken om dubbel brouwerijen (Brewer) te voorkomen.
    //  Query<...>(...).Distinct().ToList().
    
    public static List<Brewer> GetAllBrewersIncludeBeers()
    {
        string sql =
            """
                SELECT
                br.BrewerId AS BrewerId,
                br.Name As Name,
                br.Country As Country,
                '' As BeerSpilt,
                b.BeerId As BeerId,
                b.Name As Name,
                b.Type As Type,
                b.Style As Style,
                b.Alcohol As Alcohol,
                b.BrewerId As BrewerId
                From Brewer br
                LEFT JOIN Beer b ON b.BrewerId = br.BrewerId
                Order By br.Name, b.Name
            """;
        
        Dictionary<int, Brewer> brewerDictionary = new Dictionary<int, Brewer>();
        using var connection = DbHelper.GetConnection();
        List<Brewer> brewers = connection.Query<Brewer, Beer, Brewer>(
            sql,
            map: (brewer, beer) =>
            {
                if(!brewerDictionary.ContainsKey(brewer.BrewerId)) 
                {
                    brewer.Beers = new List<Beer>();
                    brewerDictionary[brewer.BrewerId] = brewer;
                }
                else
                {
                    brewer = brewerDictionary[brewer.BrewerId];
                }
                
                if (beer.BeerId != 0)
                {
                    brewer.Beers.Add(beer);
                }
                
                return brewer;
            },  
            splitOn: "BeerSpilt"
        ).Distinct().ToList();
        return brewers;
    }
    
    // 3.7 Question
    // Optioneel:
    // Dezelfde vraag als hiervoor, echter kan je nu ook de Beers property van Brewer vullen met de bieren?
    // Hiervoor moet je wat extra logica in map methode schrijven.
    // Let op dat er geen dubbelingen komen in de Beers property van Beer!
    public static List<Beer> GetAllBeersIncludeBreweryAndIncludeBeersInBrewery()
    {
        throw new NotImplementedException();
    }
    
    // 3.8 Question
    // n op n relatie (many-to-many relationship)
    // Geef een overzicht van alle cafés en welke bieren ze schenken.
    // Let op een café kan meerdere bieren schenken. En een bier wordt vaak in meerdere cafe's geschonken. Dit is een n op n relatie.
    // Sommige cafés schenken geen bier. Dus gebruik LEFT JOINS in je query.
    // Bij n op n relaties is er altijd spraken van een tussen-tabel (JOIN-table, associate-table), in dit geval is dat de tabel Sells.
    // Gebruikt de multi-mapper Query<Cafe, Beer, Cafe>("query", splitOn: "splitCol1, splitCol2").
    // Gebruik de klassen Cafe en Beer.
    // De bieren worden opgeslagen in een de property Beers (List<Beer>) van de klasse Cafe.
    // Sorteer op cafénaam en daarna op biernaam.
    
    // Kan je ook uitleggen wat het verschil is tussen de verschillende JOIN's en wat voor gevolg dit heeft voor het resultaat?
    // Het is belangrijk om te weten wat de verschillen zijn tussen de verschillende JOIN's!!!! Als je dit niet weet, zoek het op!
    // Als je dit namelijk verkeerd doet, kan dit grote gevolgen hebben voor je resultaat (je krijgt dan misschien een verkeerde aantal records).
    public static List<Cafe> OverzichtBierenPerKroegLijstMultiMapper()
    {
        string sql = 
            """
            SELECT 
                c.cafeId AS CafeId,
                c.name AS Name,
                c.Address AS Address,
                c.City AS City,
                '' As BeerSpilt,
                b.BeerId As BeerId,
                b.Name As Name,
                b.Type As Type,
                b.Style As Style,
                b.Alcohol As Alcohol,
                b.BrewerId As BrewerId
            From Cafe c
               LEFT JOIN Sells s on c.CafeId = s.CafeId
                    LEFT JOIN Beer b on s.BeerId = b.BeerId
            ORDER BY c.Name, b.Name
            """;
        
        var cafeDictionary = new Dictionary<int, Cafe>();
        using var connection = DbHelper.GetConnection();
        var cafes = connection.Query<Cafe, Beer, Cafe>(
            sql, 
            map: (cafe, beer) =>
            {
                if (!cafeDictionary.TryGetValue(cafe.CafeId, out var existingCafe))
                {
                    existingCafe =  cafe;
                    existingCafe.Beers = new List<Beer>();
                    cafeDictionary[cafe.CafeId] = existingCafe;
                }
                
                if (beer.BeerId != 0)
                {
                    existingCafe.Beers.Add(beer);
                }
                
                return existingCafe;
            }, splitOn: "BeerSpilt"
            ).Distinct().ToList();
        return cafes;
    }

    // 3.9 Question
    // We gaan nu nog een niveau dieper. Geef een overzicht van alle brouwerijen, met daarin de bieren die ze verkopen,
    // met daarin in welke cafés ze verkocht worden.
    // Sorteer op brouwerijnaam, biernaam en cafenaam. 
    // Gebruik (vul) de class Brewer, Beer en Cafe.
    // Gebruik de methode Query<Brewer, Beer, Cafe, Brewer>(...) met daarin de juiste JOIN's in de query en splitOn parameter.
    // Je zult twee dictionaries moeten gebruiken. Een voor de brouwerijen en een voor de bieren.
    public static List<Brewer> GetAllBrewersIncludeBeersThenIncludeCafes()
    {
        string sql =
            """
            SELECT
                br.BrewerId AS BrewerId,
                br.Name AS Name,
                br.Country As Country,
                
                '' AS BeerSpilt,
                b.BeerId As BeerId,
                b.Name As Name,
                b.Type As Type,
                b.Style As Style,
                b.Alcohol As Alcohol,
                b.BrewerId As BrewerId,
                
                '' AS CafeSpilt,
                c.CafeId As CafeId,
                c.Name As Name,
                c.Address As Address,
                c.City As City
            
            FROM Brewer br
            LEFT JOIN Beer b ON br.BrewerId = b.BrewerId
            LEFT JOIN Sells s on s.BeerId = b.BeerId
            LEFT JOIN Cafe c ON s.CafeId = c.CafeId
            
            ORDER BY br.Name, b.Name, c.Name
            """;
        
        var brewerDict = new Dictionary<int, Brewer>();
        var beerDict = new Dictionary<int, Beer>();
        
        using var connection = DbHelper.GetConnection();
        var result = connection.Query<Brewer, Beer, Cafe, Brewer>(
            sql, 
            (brewer, beer, cafe) =>
            {
                if (!brewerDict.TryGetValue(brewer.BrewerId, out var existingBrewer))
                {
                    existingBrewer = new Brewer
                    {
                        BrewerId = brewer.BrewerId,
                        Name = brewer.Name,
                        Country = brewer.Country,
                        Beers = new List<Beer>()
                    };
                    brewerDict.Add(brewer.BrewerId, existingBrewer);
                }

                if (!beerDict.TryGetValue(beer.BeerId, out var existingBeer))
                {
                    existingBeer = new Beer
                    {
                        BeerId = beer.BeerId,
                        Name = beer.Name,
                        Type = beer.Type,
                        Style = beer.Style,
                        Alcohol = beer.Alcohol,
                        BrewerId = beer.BrewerId,
                        Cafes = new List<Cafe>()
                    };
                    existingBrewer.Beers.Add(existingBeer);
                    beerDict.Add(beer.BeerId, existingBeer);
                }

                if (cafe.CafeId != 0)
                {  
                    existingBeer.Cafes.Add(cafe);
                }
                
                return existingBrewer;
            }, 
            splitOn: "BeerSpilt, CafeSpilt"
        ).Distinct().ToList();
        return result;
    }
    
    // 3.10 Question - Er is geen test voor deze vraag
    // Optioneel: Geef een overzicht van alle bieren en hun de bijbehorende brouwerij.
    // Sorteer op brouwerijnaam, biernaam.
    // Gebruik hiervoor een View BeerAndBrewer (maak deze zelf). Deze view bevat alle informatie die je nodig hebt gebruikt join om de tabellen Beer, Brewer.
    // Let op de kolomnamen in de view moeten uniek zijn. Dit moet je dan herstellen in de query waarin je view gebruik zodat Dapper het snap
    // (SELECT BeerId, BeerName as Name, Type, ...). Zie BeerName als voorbeeld hiervan.
    public static List<Beer> GetBeerAndBrewersByView()
    {
        throw new NotImplementedException();
    }
}