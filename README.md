# Warcaby – Unity 2D

Gra w warcaby napisana w Unity. Obsługuje tryb Gracz vs Gracz (lokalnie), Gracz vs AI oraz Online Multiplayer.

---

## ▶ Szybki start (Quick Start)

1. Otwórz folder `/warcaby` jako projekt w Unity 2022.3 LTS+
2. Poczekaj aż Unity pobierze pakiety i skompiluje skrypty
3. W górnym menu kliknij: **Tools → Warcaby → ★ SETUP EVERYTHING ★**
4. Otwórz scenę `Assets/Scenes/MainMenu.unity`
5. Naciśnij **▶ Play**

> Skrypt `SETUP EVERYTHING` automatycznie: generuje prefaby, tworzy i konfiguruje sceny Game i MainMenu, buduje UI Canvas, konfiguruje Mirror – wszystko jednym kliknięciem.

---

## Wymagania

| Narzędzie | Wersja |
|---|---|
| Unity | 2022.3 LTS + |
| .NET | Standard 2.1 |
| Mirror | ≥ 72.x (online multiplayer) |
| TextMeshPro | Wbudowane w Unity |

---

## Struktura projektu

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── PieceType.cs        – typy pionków i rozszerzenia
│   │   ├── BoardPosition.cs    – współrzędna pola (Row, Col)
│   │   ├── Move.cs             – ruch (w tym łańcuch bić)
│   │   ├── Board.cs            – stan planszy + fabryka (CreateInitial)
│   │   ├── GameRules.cs        – generator legalnych ruchów (polskie zasady)
│   │   └── GameManager.cs      – centralny kontroler gry
│   ├── AI/
│   │   └── MinimaxAI.cs        – Minimax z Alpha-Beta pruning
│   ├── Network/
│   │   ├── CheckersNetworkManager.cs   – rozszerza Mirror NetworkManager
│   │   ├── NetworkPlayer.cs            – tożsamość gracza online
│   │   └── NetworkGameManager.cs       – server-authoritative logika online
│   ├── UI/
│   │   ├── UIManager.cs        – panele, HUD, toast
│   │   └── BoardRenderer.cs    – renderer planszy i pionków
│   └── Input/
│       └── InputHandler.cs     – obsługa myszy → BoardPosition
├── Scenes/          – tu umieść sceny (MainMenu, Game)
├── Prefabs/         – prefaby kafelków, pionków, UI
├── Sprites/         – grafiki
└── Sounds/          – dźwięki
```

---

## Konfiguracja sceny (Unity Editor)

### 1. Scena Game

1. Utwórz scenę `Assets/Scenes/Game.unity`.
2. Dodaj **GameManager** GameObject → komponent `GameManager`.
3. Dodaj **BoardRenderer** GameObject → komponenty `BoardRenderer` + ustaw prefaby kafelków i pionków w Inspektorze.
4. Główna kamera → komponent `InputHandler` (`RequireComponent(Camera)`).
5. Dodaj **UIManager** GameObject → komponent `UIManager` → assignuj wszystkie UI referencje.

### 2. Scena MainMenu (opcjonalnie osobna)

Stwórz Canvas z przyciskami PvP / vs AI / Online i obsłuż ładowanie sceny Game przez `SceneManager.LoadScene("Game")`.

### 3. Online Multiplayer (Mirror)

1. Mirror jest już dodany do `Packages/manifest.json` – Unity pobierze go automatycznie przy otwarciu projektu.
2. W Unity uruchom: **Tools → Warcaby → Setup Network (Game Scene)**
   - Skrypt automatycznie:
     - Dodaje `CheckersNetworkManager` do sceny Game (zastępuje domyślny `NetworkManager`)
     - Przypisuje **NetworkPlayer** prefab
     - Przypisuje **NetworkGameManager** prefab
     - Rejestruje oba prefaby na liście Spawnable Mirror
   > **Wymaganie:** przed tym krokiem uruchom `Tools → Warcaby → Create All Prefabs`

---

## Zasady (Polskie Warcaby)

- Plansza 8×8, 12 pionków na stronę.
- Pionki poruszają się ukośnie do przodu.
- **Bicie jest obowiązkowe** (w tym wielokrotne bicia w jednym ruchu).
- Przy biciu pionki mogą bić we wszystkich 4 kierunkach.
- **Dama** powstaje po dotarciu pionka do ostatniej linii (natychmiast).
- Dama porusza się po całej przekątnej (jak goniec w szachach).
- Wygrywa gracz, który zbije lub zablokuje wszystkie pionki przeciwnika.

---

## AI – Minimax

| Poziom | Głębokość |
|---|---|
| Łatwy | 2 |
| Normalny | 5 |
| Trudny | 8 |

Funkcja oceny bierze pod uwagę:
- Wartość pionka (100 pkt) vs damy (300 pkt).
- Bonus pozycyjny (centralizacja, awans pionka).

---

## Rozbudowa

- [ ] Animacje ruchów (DOTween / coroutines)
- [ ] Dźwięki (AudioManager)
- [ ] Zapis/wczytaj partię (FEN-like format)
- [ ] Remis przez powtórzenie pozycji
- [ ] Lobby online (Photon / Mirror Relay)
