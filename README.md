HomeBuddy-API (Backend) – Huvudfunktioner och Funktionalitet

Användarautentisering & Roller: Tillhandahåller registrering och inloggning för användare med JWT-autentisering, samt ett separat inloggningsendpunkt för administratörer. Rollbaserad auktorisering används på skyddade endpoints (t.ex. endast administratörer kan nå administrationsfunktioner).

Produktkataloghantering: Hanterar en e-handelskatalog organiserad i Produktgrupper (varje grupp innehåller flera varianter). Varje produktgrupp tillhör en Kategori och kan ha flera Varianter (definierade av färg/storlek med unika SKU) samt tillhörande bilder. API:et stöder att skapa, uppdatera och ta bort produktgrupper och varianter via admin-endpoints. Det spårar även mjuk radering (soft-delete) av produkter/varianter för att kunna ta bort objekt utan att förlora data.

Lagerhantering: Bibehåller lagernivåer för varje produktvariant. Varje variant har en lagerpost med aktuell kvantitet; API:et tillhandahåller endpoints för att justera lagernivåer (t.ex. vid försäljning eller påfyllning) och loggar ändringar med hjälp av InventoryTransaction-poster. Tröskelvärden för lågt lager samt senaste påfyllningsdatum sparas för bättre lagerstyrning.

Orderhantering: Stödjer hela orderlivscykeln. Kunder kan lägga beställningar (auktoriserade användare skickar sin e-post och valda produkter), vilket genererar ett unikt ordernummer och minskar lager för varje vald variant. API:et tillåter administratörer att se alla beställningar eller filtrera per kund, uppdatera orderstatus (t.ex. markera som skickad eller annullerad – med automatisk lagerpåfyllning vid annullering), samt ta bort beställningar vid behov.

Kundrecensioner & Betyg: Implementerar ett recensionssystem där autentiserade användare kan lämna produktrecensioner med betyg, titel och kommentar. Recensioner kan hämtas publikt per produktgrupp (via slug eller ID) för att visas på produktsidor. Administratörer kan moderera recensioner (läsa alla, uppdatera eller ta bort olämpliga).

AI-genererade Recensionssammanfattningar: Integreras med OpenAI (via Azure OpenAI SDK) för att automatiskt sammanfatta produktrecensioner. Ett endpoint finns tillgängligt som samlar recensioner för en produktgrupp och returnerar en sammanfattning med genomsnittligt betyg och betygsfördelning. (För närvarande returneras en statistisk översikt, men framtida planer inkluderar GPT-genererade textresuméer.)

Ytterligare Funktioner: Inkluderar kategorihantering (fördefinierade produktkategorier som Topp, Byxor, osv., som kan hämtas via API), och hantering av adminanvändare (endpoints för att lista, skapa eller ta bort adminkonton). Backend är byggd i ASP.NET Core och använder Entity Framework Core för datahantering, vilket säkerställer starka relationer mellan entiteter (Produkter, Varianter, Ordrar, Recensioner, m.m.). Swagger används för API-dokumentation som underlättar utveckling.

LÄNK:
https://homebuddy-react-aedac9f5ckbbfmcm.norwayeast-01.azurewebsites.net/
