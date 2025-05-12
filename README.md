# Closy - Il Tuo Guardaroba Intelligente

Closy è un'applicazione web per aiutarti a gestire il tuo guardaroba, scoprire nuovi outfit e ottenere suggerimenti di stile personalizzati tramite l'intelligenza artificiale.

**Repository:** [https://github.com/LorenzoSavi/Closy](https://github.com/LorenzoSavi/Closy)

## Caratteristiche Principali (Previste)

*   **Gestione Guardaroba:** Aggiungi, modifica ed elimina capi di abbigliamento, specificando dettagli come categoria, colore, brand, stagione e occasione.
*   **Creazione Outfit Manuale:** Componi i tuoi outfit selezionando capi dal tuo guardaroba.
*   **Generazione Outfit AI:** Ottieni suggerimenti di outfit generati automaticamente dall'AI (Gemini) in base al tuo guardaroba e alle tue preferenze.
*   **Rimozione Sfondo Immagini:** (Opzionale) Funzionalità per rimuovere lo sfondo dalle immagini dei capi caricate.
*   **Autenticazione Utenti:** Registrazione e login per gestire il proprio guardaroba personale.

## Tecnologie Utilizzate

*   **Backend:** ASP.NET Core Razor Pages, C#
*   **Database:** SQL Server (o altro database compatibile con Entity Framework Core)
*   **Frontend:** HTML, CSS, JavaScript, Bootstrap (o altro framework CSS)
*   **Servizi AI:**
    *   Google Gemini API (per la generazione di outfit)
    *   Remove.bg API (per la rimozione dello sfondo, se implementata)
*   **Autenticazione:** ASP.NET Core Identity

## Setup e Installazione (Locale)

1.  **Clona la repository:**
    ```bash
    git clone https://github.com/LorenzoSavi/Closy.git
    cd Closy
    ```
2.  **Configura le stringhe di connessione e le API Keys:**
    *   Rinomina `appsettings.Development.json.example` (se presente) in `appsettings.Development.json`.
    *   Aggiorna `appsettings.Development.json` con la tua stringa di connessione al database locale e le tue chiavi API per Gemini (e Remove.bg se usata).
    *   Per una maggiore sicurezza, considera l'utilizzo di "User Secrets" per le chiavi API in ambiente di sviluppo:
        ```bash
        dotnet user-secrets init
        dotnet user-secrets set "GeminiAI:ApiKey" "LA_TUA_CHIAVE_API_GEMINI"
        # Esempio per la stringa di connessione, se non vuoi metterla direttamente in appsettings.Development.json
        # dotnet user-secrets set "ConnectionStrings:DefaultConnection" "LA_TUA_STRINGA_DI_CONNESSIONE_LOCALE"
        # Aggiungi altre chiavi API necessarie
        ```
3.  **Applica le migrazioni del database (Entity Framework Core):**
    ```bash
    dotnet ef database update
    ```
    (Potrebbe essere necessario installare `dotnet-ef` globalmente: `dotnet tool install --global dotnet-ef`)
4.  **Esegui l'applicazione:**
    ```bash
    dotnet run
    ```
    L'applicazione sarà accessibile solitamente su `https://localhost:PORTA` o `http://localhost:PORTA`.

## Deployment

Per il deployment in produzione, assicurati di configurare le variabili d'ambiente o i secrets del tuo provider di hosting (es. GitHub Secrets per GitHub Actions, Azure App Service Application Settings, ecc.) per:
*   `ConnectionStrings:DefaultConnection`
*   `GeminiAI:ApiKey`
*   `RemoveBg:ApiKey` (se utilizzata)

Non committare mai le chiavi API o le stringhe di connessione di produzione direttamente nel file `appsettings.Production.json` in una repository pubblica.

## Contribuire

Se desideri contribuire, per favore fai un fork della repository e invia una pull request.

## Licenza

Questo progetto è rilasciato sotto la Licenza MIT (o specifica la tua licenza).

---

**Nota:** Questo è un README di base. Sentiti libero di espanderlo con maggiori dettagli sul progetto, istruzioni di deployment, screenshot, ecc. man mano che il progetto cresce.
