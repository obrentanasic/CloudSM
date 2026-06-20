# Smart Metering — Cloud platforma za pametna brojila

Projekat iz predmeta **Razvoj Cloud aplikacija u pametnim mrežama** (FTN Novi Sad, 2025/2026).
Centralizovana Azure platforma koja povezuje pametna brojila (IoT uređaje) sa sistemom za
očitavanje, obračun, naplatu i nadzor mreže. Implementacija prati obrasce sa vežbi
(Čista arhitektura + DDD, Azure Functions, Table/Queue/Blob Storage, SignalR).

> Puna specifikacija se nalazi u `Spec.pdf`.

---

## Arhitektura

```
Console Simulator ──HTTP──▶ Azure Functions ──┐
 (pametno brojilo)          (RegisterDevice,    │ enqueue
                             ReceiveTelemetry)   ▼
                                           telemetry-queue
                                                 │
                                    ProcessTelemetry (Queue trigger)
                                                 │
                              ┌──────────────────┼──────────────────┐
                              ▼                   ▼                  ▼
                       Azure Table          Azure Table       meterstatus-queue
                       (Telemetries)       (MeterStatuses)          │
                                                                    ▼
React (Vite) ◀──SignalR── Web API (ASP.NET Core) ◀── MeterStatusWorker (čita queue)
 Dashboard    ◀──REST────  + Azure SQL (korisnici, objekti, brojila)
```

**Tehnologije:** .NET 8, ASP.NET Core Web API, Azure Functions (.NET 8 isolated),
EF Core + Azure SQL, Azure Table/Queue/Blob Storage, SignalR, JWT, SendGrid, React + Vite + TypeScript.

---

## Šta je urađeno (Faze 1–10)

| Faza | Opis | Status |
|------|------|--------|
| 1 | Skeleton rešenja, Čista arhitektura, domenski primitivi (`Entity`, `AggregateRoot`, `EntityId`, `BaseTableEntity`) | ✅ |
| 2 | Autentifikacija i korisnici: uloge (Admin/Consumer/BillingAdmin), JWT, postavljanje/reset lozinke, SendGrid mejlovi, EF Core + Azure SQL, seed admina | ✅ |
| 3 | Objekti i pametna brojila: registracija, CRUD, ograničenje na vlasnika, validacija serijskog broja `SM-YYYY-XXXXX` | ✅ |
| 4 | Prikupljanje telemetrije: konzolni simulator, Functions (handshake + prijem + obrada), Table Storage, `telemetry-queue`, klasifikacija tarife (VT/NT) | ✅ |
| 5 | Real-time: SignalR hub, `MeterStatusWorker`, `meterstatus-queue`, analitički REST endpointi, **React dashboard** (tabovi, kartice, grafikoni) | ✅ |
| 6 | Anomalije i notifikacije: detekcija pada napona / skoka potrošnje / offline brojila, limit potrošnje (kWh), `alert-queue` + `ProcessAlerts` (mejl), `MeterMonitor` (timer), panel za limit u dashboardu | ✅ |
| 7 | Tarifni modeli i automatizovan mesečni obračun: aktivan tarifni model (zelena/plava/crvena zona), `MonthlyBilling` timer, tekstualni i PDF računi u Blob Storage, mejl obaveštenje, digitalni karton računa u dashboardu | ✅ |
| 8 | Online plaćanje (Stripe): Checkout sesija (`/api/billing/invoices/{id}/checkout-session`) + **`StripeWebhook` Azure funkcija** koja na potvrđeno plaćanje označava račun kao plaćen | ✅ |
| 9 | Ručni unos stanja brojila: forma + slika u Blob, `OptimizeReadingImage` (Blob-trigger) za optimizaciju slike, odobravanje/odbijanje od strane administratora naplate (odobreni unos ulazi u obračun) | ✅ |
| 10 | Admin nadzor mreže: tabela statusa brojila (online/offline), pregled uplata, statistika računa (poslato mejlom), **pregled upozorenja**, te **upravljanje korisnicima** (kreiranje / suspenzija / brisanje) | ✅ |

### Funkcionalni zahtevi po poenima

| Zahtev (iz specifikacije) | Poeni | Status |
|---|---|---|
| Kreiranje naloga / prijava / JWT / reset lozinke / uloge | ~9 | ✅ |
| Upravljanje objektima | 3 | ✅ |
| Uparivanje sa IoT uređajima (registracija + handshake + token) | 6 | ✅ |
| Telemetrija i analitika (grafikoni VT/NT, napon/opterećenje, kartice) | 6 | ✅ |
| Hitna upozorenja (pad napona / nagli skok / offline) | 6 | ✅ |
| Limit potrošnje (kWh i RSD) | 6 | ✅ |
| Upravljanje tarifnim modelima | 3 | ✅ |
| Automatizovan mesečni obračun + računi | 6 | ✅ |
| Digitalni karton potrošnje (+ PDF) | 3 | ✅ |
| Online plaćanje (Stripe Checkout + Webhook funkcija) | 6 | ✅ |
| Prijava stanja brojila (slika + odobravanje) | 4 | ✅ |
| Pregled statusa / nadzor mreže (admin) | 12 | ✅ |
| Upravljanje korisničkim nalozima (kreiranje / suspenzija / brisanje) | — | ✅ |

---

## Status

Sve faze (1–10) su završene; svi funkcionalni zahtevi iz specifikacije su pokriveni, uključujući
**Stripe plaćanje** (Webhook je realizovan kao Azure funkcija, u skladu sa specifikacijom),
**ručni unos stanja** sa odobravanjem, **admin nadzor mreže** i **upravljanje korisnicima**.

Preostaje samo **objavljivanje (deploy) na Azure** (Function App + App Service); kompletna
infrastruktura (cloud, Functions, storage, queue, blob, SignalR, auth, React) i sva poslovna
logika su gotovi.

> Upozorenja se prikazuju u admin panelu (tab **„Мрежа и упозорења"**), a osim toga su vidljiva i u
> logovima `func start`, redu `alert-queue` (Storage Explorer) i kao e-mail (SendGrid) — pod uslovom
> da je primalac prava adresa (nalozi na `@smartmetering.com` se odbacuju).

---

## Preduslovi (alati)

- **Visual Studio 2022** sa workload-ovima: *Azure development* i *ASP.NET and web development*
- **.NET 8 SDK / Runtime**
- **Azure Functions Core Tools v4** (ili pokretanje Functions kroz VS) — `npm i -g azure-functions-core-tools@4`
- **Node.js 18+** i npm (za React klijent)
- **Azure for Students** nalog (https://azure.microsoft.com/free/students)
- **SendGrid** nalog (besplatan) — za mejlove
- (opciono) **Azure Storage Explorer**, **Postman**

---

## Azure resursi koje treba napraviti

U jednoj *Resource Group* (npr. `rg-smart-metering`), u istom regionu (npr. Germany West Central):

1. **Storage Account** (Standard LRS) — daje Table + Queue + Blob
2. **Azure SQL** (server + baza, Free/serverless) — uz firewall pravilo za svoju IP adresu i „Allow Azure services"
3. **Function App** (.NET 8 isolated, Consumption (Windows)) — za deploy
4. **App Service** (.NET 8) — za deploy Web API-ja

> Za **lokalno pokretanje** dovoljni su Storage Account i Azure SQL (Function App i App Service su za deploy).

---

## Konfiguracija (tajne)

⚠️ Tajne se **ne komituju** u git (fajlovi ispod su u `.gitignore`). Vrednosti zatražite od kolege
koji je postavio Azure resurse, ili upišite svoje.

### 1) `SmartMetering/WebApi/appsettings.Development.json`
```json
{
  "ConnectionStrings": {
    "SqlDatabase": "Server=tcp:<SERVER>.database.windows.net,1433;Initial Catalog=sqldb-smart-metering;User ID=<USER>;Password=<LOZINKA>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=<NALOG>;AccountKey=<KLJUC>;EndpointSuffix=core.windows.net",
  "Jwt": { "Secret": "<najmanje-32-karaktera-tajni-kljuc>" },
  "SendGrid": {
    "ApiKey": "SG.<vas-sendgrid-kljuc>",
    "FromEmail": "<verifikovani-posiljalac@example.com>",
    "FromName": "Smart Metering"
  },
  "Stripe": {
    "SecretKey": "sk_test_<stripe-sandbox-secret>",
    "WebhookSecret": "whsec_<stripe-webhook-signing-secret>",
    "SuccessUrl": "http://localhost:5173/payment-success",
    "CancelUrl": "http://localhost:5173/payment-cancel"
  },
  "AdminSeed": {
    "Email": "admin@smartmetering.com", "Password": "Admin123!",
    "FirstName": "System", "LastName": "Administrator", "PhoneNumber": "+381000000000"
  },
  "ConsumerSeed": {
    "Email": "consumer@smartmetering.com", "Password": "Consumer123!",
    "FirstName": "Test", "LastName": "Consumer", "PhoneNumber": "+381601111111"
  }
}
```

### 2) `SmartMetering/Functions/local.settings.json`
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "<ista storage konekcija kao gore>",
    "StorageConnectionString": "<ista storage konekcija kao gore>",
    "SqlConnectionString": "<ista SQL konekcija kao gore>",
    "SendGridApiKey": "SG.<vas-sendgrid-kljuc>",
    "SendGridFromEmail": "<verifikovani-posiljalac@example.com>",
    "SendGridFromName": "Smart Metering",
    "StripeSecretKey": "sk_test_<stripe-sandbox-secret>",
    "StripeWebhookSecret": "whsec_<stripe-webhook-signing-secret>",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```
> `SendGrid*` ključevi su potrebni `ProcessAlerts` funkciji za slanje mejlova upozorenja i
> `MonthlyBilling` funkciji za slanje obaveštenja o novim računima. Računi se čuvaju u Blob
> container-u `invoices`, koji aplikacija kreira automatski.
>
> `Stripe*` ključevi su potrebni `StripeWebhook` funkciji (potvrda plaćanja). Webhook je realizovan
> kao Azure funkcija na ruti `POST /api/stripe-webhook` — u Stripe Dashboard-u podesi endpoint na
> `https://<function-app>/api/stripe-webhook` (lokalno: `http://localhost:7071/api/stripe-webhook`,
> npr. preko `stripe listen --forward-to`). Kreiranje Checkout sesije ostaje u Web API-ju, pa Web API
> i dalje koristi `Stripe:SecretKey`.

### 3) `client/.env`
```
VITE_API_BASE_URL=http://localhost:5098
```

---

## Pokretanje (lokalno)

Prvi put, primeni EF migracije na bazu (kreira tabele):
```powershell
cd SmartMetering
dotnet tool install --global dotnet-ef   # samo prvi put
dotnet ef database update --project Infrastructure --startup-project WebApi
```

Zatim pokreni komponente (svaka u svom terminalu):

**1. Web API** (REST + SignalR) — mora na portu **5098** (klijent ga tu očekuje, vidi `client/.env`):
```powershell
cd SmartMetering
dotnet run --project WebApi --urls http://localhost:5098
```
Swagger: http://localhost:5098/swagger

**2. Azure Functions** (prijem telemetrije):
```powershell
cd SmartMetering/Functions
func start --port 7071
```

**3. Simulator** (šalje merenja) — prvo registruj brojilo kroz dashboard, pa:
```powershell
cd SmartMetering
$env:FUNCTIONS_BASE_URL="http://localhost:7071"
dotnet run --project Simulator
# unesi serijski broj (npr. SM-2026-00001) i tip prikljucka (1 = mono, 3 = trofazno)
```

**4. React klijent**:
```powershell
cd client
npm install   # samo prvi put
npm run dev
```
Otvori **http://localhost:5173**.

### Test nalozi (kreiraju se automatski u Development modu)
- **Administrator:** `admin@smartmetering.com` / `Admin123!`
- **Potrošač:** `consumer@smartmetering.com` / `Consumer123!`

### Brzi end-to-end test
1. Prijavi se kao **potrošač** → napravi objekat → registruj brojilo `SM-2026-00001` (monofazno).
2. Pokreni **Functions** i **Simulator** (sa tim serijskim brojem).
3. Na dashboardu izaberi tab objekta → kartica brojila i grafikoni se ažuriraju **uživo** (SignalR).

### Obračun i računi (Faza 7)
1. Prijavi se kao **administrator** (`admin@smartmetering.com` / `Admin123!`).
2. Na admin ekranu kreiraj i aktiviraj tarifni model. Standardni pragovi iz specifikacije su:
   zelena zona do `350 kWh`, plava zona do `1200 kWh`, crvena zona preko `1200 kWh`.
3. Prijavi se kao **potrošač**, napravi objekat i upari brojilo, zatim pusti simulator da napravi telemetriju.
4. Za test bez čekanja prvog u mesecu, vrati se kao administrator i u panelu **Ručni obračun**
   izaberi godinu/mesec i klikni **Generiši račune**.
5. Kao potrošač, u izabranom objektu otvori **Digitalni karton računa**. Tu se vide računi
   od najnovijeg ka najstarijem, potrošnja po VT/NT i zonama, status i dugme za PDF.

### Upravljanje korisnicima (Faza 10, samo Administrator)
Prijavi se kao **administrator** i otvori tab **Корисници**. Tu možeš da:
- **kreiraš nalog** (ime, prezime, imejl, telefon, uloga) — sistem šalje aktivacioni mejl sa linkom
  za postavljanje lozinke (nema javne registracije);
- **suspenduješ / reaktiviraš** nalog (suspendovan korisnik ne može da se prijavi);
- **obrišeš** nalog bez povezanih podataka. Korisnik koji ima objekte/račune/očitavanja ne može
  da se obriše (FK `Restrict`) — umesto toga ga suspenduj.

> Tab **Корисници** je vidljiv samo ulozi `Admin`; `BillingAdmin` vidi naplatu i nadzor mreže.

### Plaćanje (Stripe, Faza 8)
1. U `Stripe` sekciji podesi `SecretKey` (Web API) i `WebhookSecret` (Functions) iz Stripe **Sandbox**-a.
2. Pokreni Web API i Functions, a webhook izloži Stripe-u (npr. `stripe listen --forward-to
   http://localhost:7071/api/stripe-webhook`).
3. Kao potrošač u **Digitalnom kartonu** klikni **Плати** na neplaćenom računu → Stripe Checkout.
4. Po uspešnom plaćanju `StripeWebhook` Azure funkcija označava račun kao **Плаћен**
   (vidljivo i u admin pregledu uplata).

Automatski obračun radi u Azure Functions projektu preko timer funkcije `MonthlyBilling`.
Cron izraz je `0 0 2 1 * *`, što znači da prvog dana u mesecu u 02:00 UTC generiše račune
za prethodni mesec. Funkcija prolazi kroz sva **uparena** brojila, primenjuje aktivni tarifni
model, čuva `.txt` i `.pdf` račun u Blob Storage i šalje mejl potrošaču ako je SendGrid podešen.

Ako nema aktivnog tarifnog modela, obračun se preskače i u logu Functions procesa vidi se
upozorenje. RSD limit potrošnje koristi isti aktivni tarifni model; kWh limit radi i bez tarife.

> Napomena: Azure SQL Free (serverless) se „uspava" kad nije u upotrebi — prvi zahtev nakon pauze
> može potrajati ~30–60 s dok se baza ne probudi (aplikacija automatski ponavlja pokušaj).

---

## Struktura projekta

```
SmartMetering/
├── Domain/          # entiteti, agregati, vrednosni objekti, enumeracije
├── Application/     # interfejsi (repozitorijumi/servisi), servisi, DTO-ovi
├── Infrastructure/  # EF Core, Table/Queue/Blob repozitorijumi, JWT, SendGrid, mapiranja
├── Functions/       # Azure Functions (RegisterDevice, ReceiveTelemetry, ProcessTelemetry, MeterMonitor, ProcessAlerts, MonthlyBilling, OptimizeReadingImage, StripeWebhook)
├── WebApi/          # REST kontroleri, SignalR hub, background worker, autentifikacija
└── Simulator/       # konzolna aplikacija — simulator pametnog brojila
client/              # React + Vite + TypeScript dashboard
Spec.pdf             # specifikacija projekta
```
