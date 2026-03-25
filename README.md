# HomeBuddy API (Backend)

ASP.NET Core Web API för HomeBuddy e‑handel. Backend ansvarar för autentisering, katalog (grupper/varianter), lager, order, favoriter, recensioner samt admin‑funktioner.

## Snabbstart (lokalt)

1. Konfigurera connection string för SQL Server:
   - `ConnectionStrings:DefaultConnection`
2. Konfigurera JWT:
   - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
3. Kör migreringar och starta:

```bash
dotnet ef database update
dotnet run
```

Swagger finns vanligtvis på `https://localhost:7039/swagger`.

## Modell (enkelt beskrivet)

- **Category** → har många **ProductGroup**
- **ProductGroup** → “produkten” i butiken (namn/slug/kategori) och samlar varianter/bilder
- **Variant** → köpbar enhet (SKU, color, size, `Price`, optional `ListPrice` för rabatt)
- **Inventory** → lager per variant + transaktionslogg via **InventoryTransaction**
- **Order** + **OrderItem** → order med status och momsfält (subtotal/tax/total)
- **UserFavorite** → önskelista/favoriter per användare
- **Review** → recensioner kopplade till produktgrupp

## Autentisering & roller (JWT)

Systemet använder JWT + rollbaserad auktorisering:
- **User**: kundfunktioner (profil, favoriter, recensioner, sparade leveransuppgifter).
- **Admin**: admin‑endpoints (katalog, orders, users, dashboard, kampanjer).

Auth-endpoints:
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/admin/login`
- `GET /api/auth/me` (**server‑validerad** identitet för frontend)

### Lösenord (bcrypt)
Lösenord lagras som **bcrypt**. Legacy‑konton med tidigare HMAC‑hash uppgraderas automatiskt till bcrypt vid lyckad inloggning.

## Forgot / Reset password (förberedda för e‑posttjänst)

- `POST /api/auth/forgot-password`: skapar engångstoken (hash lagras i DB, expirerar, single‑use).  
  - I **Development** kan raw token returneras för test utan e‑post.
  - I produktion är tanken att token skickas via extern e‑posttjänst som en reset‑länk till frontend.
- `POST /api/auth/reset-password`: tar `email + token + newPassword` och byter lösenord.

## Katalog & listing (publikt)

- `GET /api/categories`
- `GET /api/groups/{slugOrObjectId}`
- `GET /api/products` (sök/filter/sort/pagination)
- `GET /api/products/deals` (endast rabatterade varianter: `ListPrice > Price` + i lager)

## Lager

- Lager per variant i **Inventory**.
- Order skapar lagertransaktion (sale).
- Cancel kan återställa lager (restock).

## Orders

- `POST /api/orders` skapar order och beräknar moms via landkod.
- `PUT /api/orders/{id}` admin uppdaterar status (Pending/Paid/Shipped/Cancelled).
- `POST /api/orders/claim` kopplar order till konto (matchar e‑post).

## Favoriter (wishlist)

- `GET /api/favorites`
- `POST /api/favorites`
- `DELETE /api/favorites/{variantId}`

## Adminpanel (API) – exempel

- `GET/POST/PUT/DELETE /api/admin/groups`
- `GET/POST/PUT/DELETE /api/admin/variants`
- `GET /api/admin/dashboard/summary`

### Kampanjer / rabatter (gruppnivå)

Rabattmodellen:
- `Price` = pris kunden betalar
- `ListPrice` = ordinarie pris (visas överstruket när `ListPrice > Price`)

Admin-endpoints:
- `POST /api/admin/groups/{id}/discount` (rabattera hela gruppen)
- `POST /api/admin/groups/{id}/discount/remove` (avrabattera)

## Kravspec – uppfyllelse (kort)

✅ Finns i kod:
- JWT auth + roller (User/Admin)
- Katalog: grupper/varianter, kategorier, bilder, lager
- Sök/filter/sort/pagination
- Orders + orderstatus + historik/claim
- Momsberäkning (TaxController)
- Adminpanel (produkter, ordrar, användare, dashboard)
- Prislogik (rabatter/kampanjpris via `ListPrice`)

⚠️ Delvis:
- Prestandakrav (ingen lasttest)
- Säkerhets-/tillgänglighetskrav som process (OWASP/WCAG audit)
- Arkitektur enligt strikt lagerindelning (Application/Domain/Infrastructure)

❌ Kräver externa integrationer:
- Betalningsleverantör + webhooks
- E‑posttjänst (orderbekräftelse + reset‑länk i prod)
- Frakt/tracking
- Returer/återbetalningar

## Länk

`https://homebuddy-react-aedac9f5ckbbfmcm.norwayeast-01.azurewebsites.net/ "(ur funktion för tillfället)"`
