# ViMultiSync / ViSyncMaster

## Zgłaszanie wad jakościowych (VRSKT) – jak to działa

Poniższy opis dotyczy popupu **Wady / Awarie / Production Issues** w trybie `VRSKT`.

### 1) Przepływ w UI

1. Operator otwiera popup „Production Issues”.
2. Włącza się flow jakościowy (`VrsktQualityFlowActive`).
3. Operator wybiera:
   - **typ elementu**,
   - **powód jakościowy**.
4. Jeśli wybrana kategoria to „Inne”, wymagany jest dodatkowy **własny opis**.
5. Po poprawnym wysłaniu widoczny jest zielony komunikat sukcesu w popupie.
6. Kliknięcie poza popup zamyka go i resetuje cały flow wyboru.

---

### 2) Co jest wymagane do wysłania zgłoszenia

Aby zgłoszenie zostało wysłane, muszą być dostępne:

- `ActiveProductNumber` (numer aktualnie/ostatnio produkowanej sztuki),
- `SelectedVrsktQualityElementType` (typ elementu),
- `SelectedVrsktQualityReason` (powód jakościowy),
- dla kategorii „Inne”: `VrsktQualityCustomDescription` (opis własny).

Jeżeli czegoś brakuje, aplikacja pokaże popup z listą brakujących pól.

---

### 2.1) Format tekstów wad (spacje w nazwach)

Dla zgłoszeń jakościowych aplikacja zachowuje oryginalne nazwy z UI, np.:

- `Uszkodzenia mechaniczne`
- `Wady dostawców`
- `Sensory, komponenty`

Czyli raport do Splunka zawiera normalne spacje w nazwach typu/powodu (bez „sklejania” słów).

---

### 3) Wysyłka do Splunka (bezpośrednio, bez zapisu do DB)

Zgłoszenie jakościowe (`QualityIssueReportMessage`) jest wysyłane **bezpośrednio do Splunka** przez:

- `MainWindowViewModel.SendVrsktQualityReportAsync()`
- `MainWindowViewModel.SendMessageToSplunk<T>()`
- `GenericSplunkLogger<T>.LogAsync()` → HTTP POST do HEC Splunk.

Dla samych zgłoszeń jakościowych nie ma pośredniego zapisu „do DB i dopiero wysyłka”.

> Uwaga: aplikacja może odczytywać dane pomocnicze (np. numer produktu) z już dostępnych danych runtime/cache, ale samo zgłoszenie jakościowe wysyłane jest ścieżką direct-to-Splunk.

---

### 4) Minimalna checklista operatorska

Przed kliknięciem „Wyślij zgłoszenie”:

- wybierz kategorię elementu,
- wybierz powód,
- dla „Inne” wpisz opis,
- upewnij się, że system ma numer aktualnej sztuki.

Jeżeli pojawi się błąd braków danych – popup podaje dokładnie, czego brakuje.
