# Closy - Il tuo Guardaroba Digitale

Closy è un'applicazione web progettata per aiutarti a gestire il tuo guardaroba digitale in modo semplice ed efficiente. Grazie a Closy, puoi:

- Tenere traccia dei tuoi capi d'abbigliamento.
- Creare e salvare outfit personalizzati.
- Esplorare il tuo guardaroba in modo organizzato.
- Personalizzare l'interfaccia con un tema visivo unico.
- **Amministrare utenti (per amministratori)**: Visualizzare e gestire gli utenti registrati.

---

## Funzionalità Principali

### Dashboard
La dashboard fornisce una panoramica dei tuoi capi e outfit preferiti, con azioni rapide per aggiungere capi o creare nuovi outfit.

### Gestione dei Capi
- Visualizza tutti i capi del tuo guardaroba.
- Aggiungi nuovi capi con foto, categorie e descrizioni.

### Creazione degli Outfit
Crea outfit combinando i tuoi capi e salvali per un utilizzo futuro.

### Tema Personalizzabile
Modifica il tema visivo dell'applicazione per adattarlo ai tuoi gusti.

### Gestione Utenti (Solo per Amministratori)
Gli amministratori hanno accesso a una sezione dedicata per:
- Visualizzare l'elenco degli utenti registrati.
- Gestire i ruoli e le autorizzazioni di ciascun utente.

*(Inserire immagine della pagina di amministrazione utenti qui - "admin_users.png")*

---

## Installazione

### Prerequisiti
- .NET 6 o superiore
- SQLite (per il database locale)

### Istruzioni
1. Clona il repository:
   ```bash
   git clone https://github.com/tuo-username/closy.git
   cd closy
   ```

2. Configura il database in `appsettings.json`:
   ```json
   "ConnectionStrings": {
       "DefaultConnection": "Data Source=closy.db"
   }
   ```

3. Esegui le migrazioni del database:
   ```bash
   dotnet ef database update
   ```

4. Avvia l'applicazione:
   ```bash
   dotnet run
   ```

5. Apri il browser all'indirizzo:
   ```
   http://localhost:7000
   ```

---

## Contribuire

Vuoi contribuire a Closy? Segui questi passaggi:
1. Forka il repository.
2. Crea un branch:
   ```bash
   git checkout -b nome-branch
   ```
3. Invia una pull request descrivendo le modifiche.

---

## Licenza

Closy è distribuito sotto la licenza MIT. Per ulteriori dettagli, consulta il file [LICENSE](LICENSE).
