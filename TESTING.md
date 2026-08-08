# Kako ovo testirati ručno

Ovim scenarijima je aplikacija prolazena pri testiranju, na razvojnoj mašini. Zapisani su zato što
se kroz konzolnu aplikaciju teško prati šta se sve desilo, pa je lakše kad uz svaki korak stoji i
šta bi trebalo da se ispiše. Ako se ispiše nešto drugo, to je nalaz.

```bash
dotnet run --project src/EventBooking.ConsoleApp
```

Podaci žive u memoriji. Zatvaranje aplikacije vraća sve na početno stanje, pa se slobodno može
griješiti i počinjati ispočetka.

Scenariji se oslanjaju jedan na drugi i najbolje ih je proći redom. Gdje neki korak mijenja brojke u
kasnijem koraku, to je posebno naglašeno.

---

## Vrijeme: ekran je lokalan, domen je UTC

Prva stvar koja zna zbuniti. Termini događaja se čuvaju u UTC, a ekran ih prikazuje u lokalnoj zoni
(`Format.Moment` radi `ToLocalTime()`). U zoni UTC+2 događaj koji u domenu traje 10:00-16:00 na
ekranu piše 12:00-18:00.

To znači da se pri računanju "je li događaj prošao" ne smije porediti sat sa ekrana sa terminom iz
koda. Ako izgleda da je zakazani posao nešto propustio, prvo treba provjeriti zonu.

---

## Šta je u demo podacima

Četiri događaja, izabrana tako da svaki pokriva drugo pravilo:

| Događaj | Tip | Počinje za | Zašto postoji |
|---|---|---|---|
| Domain-Driven Design u praksi | Radionica | 30 sati | traži 8 polaznika, prodane su 2, otkazat će se sama |
| Clean Architecture radionica | Radionica | 20 dana | rasprodana, polazna tačka za listu čekanja |
| Dubioza kolektiv - Sarajevo Live | Koncert | 45 dana | dovoljno daleko da važi early bird popust |
| .NET Days BiH 2026 | Konferencija | 60 dana | dva dana, tri sesije, jedno kolo karata sa rokom prodaje |

Šest korisnika: četiri kupca (Lejla Begic je Gold, Emir Saric Silver, Nina Maric i Tarik Delic
Standard) i dva organizatora (Amila Hodzic iz Skyline Eventsa, Damir Kovacevic iz BH Konferencija).

Aplikacija traži prijavu prije nego što išta pokaže. Adrese su ispisane na ekranu za prijavu, jer je
ovo demo; lozinke nema, vidi obrazloženje u [README](README.md#šta-je-svjesno-izostavljeno).

Za scenarije niže prijavi se kao **`lejla.begic@example.ba`** (kupac, Gold). Meni koji dobiješ
pokazuje samo ono što njena rola smije.

Prije toga vrijedi provjeriti i odbijanje: upiši `nepostoji@example.ba`, pa `bez-etice`. Oba puta
mora doći **isti** odgovor, `No account for that address.` Različiti odgovori bi strancu odali koje
adrese postoje.

---

## 1. Katalog i filteri

Izbor `1`, pa četiri puta Enter (preskakanje filtera) i `n` na pitanje o rasprodanim događajima.

Očekuje se **3 događaja**, sortirana po datumu: DDD radionica, Dubioza, .NET Days. Rasprodana
radionica nedostaje jer je filter `OnlyBookable` uključen.

Isto ponoviti sa `y` na zadnjem pitanju, sada se pojavljuje i četvrti, rasprodani.

Vrijedi probati i pojedinačne filtere: tekst `Dubioza` vraća samo koncert, maksimalna cijena `50`
izbacuje konferenciju i radionice.

## 2. Rezervacija i slaganje popusta

Iz rezultata otvoriti **Dubioza kolektiv**, pa `1` (Book tickets) i unijeti `2` Partera, `0` Tribine,
`1` VIP ložu.

Očekuje se razrada u kojoj se na svaku stavku primjenjuju **oba** pravila:

```
Subtotal   210.00 BAM
Discounts  -52.50 BAM
Total      157.50 BAM
```

Popusti su `Early bird (30 days ahead): -15%` i `Loyalty: -10%`, koncert je 45 dana daleko, a Lejla
je Gold. Pravila se slažu, ne biraju.

Iznad razrade piše da su mjesta zadržana do određenog trenutka. To je neplaćeni hold: mjesta niko
drugi ne može uzeti, ali se sama oslobađaju ako plaćanje ne stigne. Potvrditi sa `y`.

## 3. Pravila koja moraju odbiti

Ovo su namjerni promašaji, sistem ih mora odbiti obrazloženom porukom, a ne pući.

| Pokušaj | Očekivano |
|---|---|
| 3 VIP lože na koncertu | odbija, najviše 2 VIP po rezervaciji |
| ukupno više od 6 karata | odbija, prekoračen maksimum po rezervaciji |
| prijava kao Amila, pa upravljanje konferencijom `.NET Days` | konferencija se ne pojavljuje u njenoj listi, nije njen događaj |

Zadnji red je važan: vlasništvo se ne provjerava u ekranu nego u domenu, pa se do tuđeg događaja ne
može doći ni zaobilaznim putem.

## 4. Otkazivanje i povrat

Meni `2` (My bookings), otvoriti rezervaciju iz scenarija 2.

Prije potvrde se ispisuje koliko bi povrat iznosio **u tom trenutku**. Politika koncerta ima tri
stepenice: 100% na 14 i više dana unaprijed, 50% na 3 i više, ništa nakon toga. Koncert je 45 dana
daleko, pa se očekuje pun povrat od 157.50 BAM.

Otkazati sa `y` i unijeti razlog.

## 5. Notifikacije

Meni `3`, pa `y` za samo svoje poruke.

Očekuju se potvrda rezervacije i obavijest o otkazivanju, sa razlogom i iznosom povrata. Sa `n`
se vide sve poruke, ukupno ih je više nego onih upućenih prijavljenom korisniku, što pokazuje da
inbox filtrira po primaocu.

Poentu vrijedi zapamtiti: nijedan servis ne zna da notifikacije postoje. Svaku poruku je napravio
rukovalac koji sluša domenski događaj.

## 6. Role i izvještaji

Meni `5` (Sign out), pa se prijavi kao **`amila.hodzic@skyline-events.ba`**, organizatorica u
Skyline Eventsu.

Meni se mijenja: nestaju "Browse and book events" i "My bookings", pojavljuju se "Organiser" i
"Reports". Isti program, druga prava, a ekrani se ne brane sami, nego ih meni ni ne ponudi.

`2` otvara izvještaje. Brojke se moraju zbrajati: neto po događaju je bruto minus povrati, a
ukupan neto prihod je zbir neto iznosa svih događaja.

`1` otvara upravljanje događajima. Ovdje se može napraviti novi događaj, dodati mu vrsta karte i
objaviti ga, pa se vratiti kao kupac i provjeriti da se pojavio u katalogu.

## 7. Vrijeme i zakazani posao

Najzanimljiviji dio, i zato ide zadnji: pomjeranje sata mijenja sve ostalo i ne vraća se samo.

**Meni `4`, pa `1` (Advance 20 minutes).** Očekuje se izvještaj o održavanju:

```
Holds expired      1
Events cancelled   1
Bookings refunded  1
```

Neplaćeni hold iz demo podataka je propao, a DDD radionica se **otkazala sama**: traži 8 polaznika,
prodane su 2, a počinje za manje od 48 sati. Razlog otkazivanja koji stoji u obavijesti
(`Only 2 of the required 8 attendee(s) signed up.`) sastavio je domen, ne ekran.

U izvještajima se nakon toga očekuje da otkazana radionica pokazuje **`0/20` i `0%`**, mjesta su
vraćena u ponudu, te `Tickets sold 18` u sažetku. Brojke važe ako je rezervacija iz scenarija 2
otkazana u scenariju 4; ako nije, veće su za te karte.

**Zatim `4`, pa `3` (Advance 20 days).** Koncert je sada 25 dana daleko, ispod praga za early bird.
Ponovnim rezervisanjem 2 Partera dobija se:

```
2 x Parter @ 45.00 BAM = 90.00 BAM
    Loyalty: -10% = -9.00 BAM
Total  81.00 BAM
```

Nema reda `Early bird`. Pravilo se ugasilo samo od sebe, bez ijedne izmjene u kodu. Vrijeme ulazi
kroz `IClock`, pa se ovo može posmatrati umjesto da se uzima na riječ.

Opcija `6` vraća sat na stvarno vrijeme ako treba počinjati ispočetka.

---

Opis arhitekture, dizajnerskih odluka i svjesnih kompromisa je u [README](README.md).
