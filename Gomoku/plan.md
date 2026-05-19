# Gomoku Mini-Game — Implementation Plan

## Overview

A browser-based Gomoku (Five in a Row) game for two players, built with HTML5 Canvas, Bootstrap 5, and jQuery. No server required — single HTML file that runs locally.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Markup | HTML5 |
| Styling | Bootstrap 5 (CDN) |
| Canvas drawing | HTML5 Canvas API |
| Logic / Events | jQuery 3.x (CDN) |

---

## File Structure

```
Gomoku/
├── plan.md          ← this file
└── index.html       ← single-file game (HTML + CSS + JS inline)
```

---

## UI Layout (Bootstrap Grid)

```
┌──────────────────────────────────────────────┐
│            GOMOKU  五子棋                     │  ← navbar / header
├────────────────────┬─────────────────────────┤
│                    │  Status Panel           │
│   Game Board       │  ─────────────────────  │
│   (Canvas)         │  ● Black's turn         │
│   15×15 grid       │                         │
│                    │  Score                  │
│                    │  Black: 0  White: 0     │
│                    │                         │
│                    │  [New Game]  [Undo]      │
│                    │                         │
│                    │  Rules (collapsible)    │
└────────────────────┴─────────────────────────┘
```

- Left column (`col-lg-8`): Canvas board, responsive, square-constrained
- Right column (`col-lg-4`): Status, scores, controls

---

## Board Rendering (Canvas)

- Board: **15 × 15** intersections (standard Gomoku size)
- Cell size: **40 px** → canvas = **600 × 600 px** (14 gaps × 40 + 2 × padding)
- Draw grid lines connecting all intersections
- Mark the 5 star points (center + 4 symmetric) with small filled circles
- Coordinate labels (A–O) along top edge and (1–15) along left edge

### Stone rendering
- Black stone: radial gradient dark circle with subtle highlight
- White stone: radial gradient light circle with subtle shadow
- Last-move marker: small red dot at stone center

---

## Data Model

```javascript
// State object (plain JS, managed via jQuery events)
state = {
  board: Array(15).fill(null).map(() => Array(15).fill(0)),
  // 0 = empty, 1 = black, 2 = white
  currentPlayer: 1,          // 1 = black, 2 = white
  gameOver: false,
  winner: null,
  moveHistory: [],           // [{row, col, player}, ...] for undo
  scores: { black: 0, white: 0 }
}
```

---

## Core Functions

| Function | Responsibility |
|----------|---------------|
| `initGame()` | Reset board state, clear canvas, draw empty grid |
| `drawBoard()` | Render grid lines and star points on canvas |
| `drawStones()` | Iterate board array and draw each stone |
| `drawLastMoveMark()` | Place red dot on most recent stone |
| `handleClick(e)` | Convert mouse coordinates → board cell, place stone |
| `placeStone(row, col)` | Update state, push to history, redraw, check win |
| `checkWin(row, col)` | Scan 4 directions from last move for 5-in-a-row |
| `scanDirection(row, col, dr, dc)` | Count consecutive stones in one axis |
| `undoMove()` | Pop last move, restore board state, redraw |
| `updateStatus()` | Refresh turn indicator, winner banner via jQuery |
| `highlightWinLine(cells)` | Draw colored line over winning 5 stones |

---

## Win Detection Algorithm

After each stone placement at `(r, c)`:
1. Check 4 directions: horizontal `(0,1)`, vertical `(1,0)`, diagonal `(1,1)`, anti-diagonal `(1,-1)`
2. For each direction, count consecutive stones of the same color in both directions from `(r, c)`
3. If count ≥ 5 → game over, announce winner, highlight winning line
4. If all 225 cells filled and no winner → draw

---

## User Interactions

| Action | Trigger | Behavior |
|--------|---------|----------|
| Place stone | Click on canvas | Snap to nearest intersection, reject if occupied or game over |
| Hover | Mousemove on canvas | Show ghost stone (semi-transparent) at nearest empty cell |
| New Game | Button click | Confirm dialog → `initGame()` |
| Undo | Button click | Remove last stone, switch back to previous player |
| Rules toggle | Button / accordion | Bootstrap collapse panel |

---

## Styling Details

- Dark wood-texture background for canvas area (`#c8a96e` base color, CSS radial gradient)
- Bootstrap dark navbar with game title
- Player turn badge: Bootstrap `badge bg-dark` or `badge bg-light text-dark`
- Winner alert: Bootstrap `alert alert-success` with animation
- Responsive: canvas scales via CSS `max-width: 100%` + `aspect-ratio: 1`

---

## Implementation Steps (ordered)

1. **Scaffold** — HTML shell with Bootstrap CDN, jQuery CDN, canvas element, sidebar layout
2. **Board drawing** — `drawBoard()`: grid lines, star points, coordinate labels
3. **State init** — `initGame()` resets JS state and redraws blank board
4. **Click handling** — map pixel click → grid cell, validate, call `placeStone()`
5. **Stone rendering** — gradient circles, last-move marker
6. **Win detection** — `checkWin()` with 4-direction scan
7. **Win UI** — highlight line, show winner alert, disable further clicks
8. **Undo** — history stack, pop and redraw
9. **Hover ghost** — mousemove ghost stone for UX polish
10. **Scores & status** — jQuery DOM updates for turn, scores
11. **Responsive sizing** — CSS to make canvas fit mobile screens
12. **Final polish** — animations, color scheme, rules accordion

---

## Out of Scope (for this version)

- AI opponent (computer player)
- Online multiplayer
- Timer / clock
- Move notation export
