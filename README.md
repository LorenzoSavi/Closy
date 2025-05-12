# Closy - Il tuo guardaroba digitale

Closy è un'applicazione web progettata per gestire il tuo guardaroba digitale. Con Closy, puoi:
- Tenere traccia di tutti i tuoi capi d'abbigliamento.
- Creare outfit personalizzati combinando i tuoi capi.
- Esplorare il tuo guardaroba in modo semplice e veloce.
- Salvare i tuoi capi e outfit preferiti.
- Personalizzare il tema visivo per un'esperienza utente unica.

---

## Funzionalità principali

### Dashboard
La dashboard è il punto di partenza dopo l'accesso. Qui puoi vedere:
- **I tuoi capi preferiti**: Una sezione dedicata ai capi che ami di più.
- **I tuoi outfit preferiti**: Una panoramica degli outfit che hai salvato come preferiti.
- **Azioni rapide**: Pulsanti per aggiungere un capo, creare un outfit o esplorare il guardaroba.

---

### Gestione dei capi
- **Tutti i capi**: Visualizza l'elenco completo dei tuoi capi d'abbigliamento.
- **Aggiungi un capo**: Carica un nuovo capo al tuo guardaroba con foto, categoria e descrizione.

![Tutti i capi](img/garments.png)
![Aggiungi capo](imag/add_garment.png)

---

### Creazione di outfit
Crea outfit combinando i capi presenti nel tuo guardaroba:
- **Selezione dei capi**: Scegli i capi che vuoi includere nell'outfit.
- **Salvataggio**: Salva l'outfit per rivederlo e modificarlo in seguito.

![Crea outfit](img/create_outfit.png)


## Funzionalità Principali

### Dashboard
La dashboard fornisce una panoramica dei tuoi capi e outfit preferiti, con azioni rapide per aggiungere capi o creare nuovi outfit.

### Gestione dei Capi
- Visualizza tutti i capi del tuo guardaroba.
- Aggiungi nuovi capi con foto, categorie e descrizioni.

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
