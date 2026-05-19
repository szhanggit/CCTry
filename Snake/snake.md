# Snake Game Design

## Overview

A classic Snake game in a single HTML file using HTML5 Canvas for rendering, Bootstrap 5 for UI styling, and jQuery for event handling and DOM updates.

## File Structure

```
snake.html   — single self-contained file (HTML + CSS + JS)
```

## Tech Stack

| Layer | Choice | Reason |
|-------|--------|--------|
| Markup | HTML5 | Semantic layout |
| Rendering | `<canvas>` | Efficient per-frame drawing; avoids DOM thrashing |
| Styling | Bootstrap 5 (CDN) | Ready-made buttons, badges, cards, grid |
| Logic / Events | jQuery 3 (CDN) | Simplified event binding and DOM updates |

## Layout (Bootstrap Grid)

```
+-----------------------------------------------+
|           Snake  [Score: 0]  [High: 0]        |  ← navbar / header
+-----------------------------------------------+
|                                               |
|          +-------------------------+          |
|          |                         |          |
|          |      <canvas>           |          |
|          |   (400 × 400 px)        |          |
|          |                         |          |
|          +-------------------------+          |
|                                               |
|         [Start]  [Pause]  [Restart]           |  ← Bootstrap buttons
|                                               |
|     Arrow keys to move · Eat food to grow    |  ← hint text
+-----------------------------------------------+
```

## Game Constants

| Constant | Value | Notes |
|----------|-------|-------|
| `COLS` | 20 | Grid columns |
| `ROWS` | 20 | Grid rows |
| `CELL` | 20 px | Cell size (canvas = 400 × 400) |
| `INITIAL_SPEED` | 150 ms | Interval between ticks |
| `SPEED_STEP` | 5 ms | Interval shrinks by this every 5 food eaten |
| `MIN_SPEED` | 60 ms | Fastest possible interval |

## Data Structures

### Snake
```
snake: [ {x, y}, {x, y}, ... ]
```
- Index 0 is the head.
- Each tick the head moves in `nextDir`; the tail segment is removed unless food was just eaten.

### Direction
```
dir:     {dx, dy}   — current direction being executed this tick
nextDir: {dx, dy}   — queued from the last keypress (prevents 180° reversal)
```

### Food
```
food: {x, y}   — random cell not occupied by the snake
```

### Game State
```
state: 'idle' | 'playing' | 'paused' | 'gameover'
```

## Game Loop

```
startLoop()
  └─ setInterval(tick, speed)

tick()
  1. Compute new head = head + nextDir
  2. dir = nextDir
  3. Check wall collision  → game over
  4. Check self collision  → game over
  5. If new head == food:
       a. grow snake (don't remove tail)
       b. score += 10
       c. place new food
       d. maybe increase speed
     Else:
       remove tail segment
  6. Unshift new head onto snake array
  7. draw()
  8. updateScore()
```

## Rendering (Canvas)

Each call to `draw()`:
1. Fill canvas with dark background (`#1a1a2e`).
2. Draw grid lines (subtle, `#16213e`).
3. Draw food — red circle inscribed in cell.
4. Draw snake body segments — rounded green rectangles.
5. Draw snake head — brighter green, slightly larger rounded rect.

## Input Handling (jQuery)

```js
$(document).on('keydown', function(e) {
  switch (e.key) {
    case 'ArrowUp':    setDir( 0, -1); break;
    case 'ArrowDown':  setDir( 0,  1); break;
    case 'ArrowLeft':  setDir(-1,  0); break;
    case 'ArrowRight': setDir( 1,  0); break;
  }
  e.preventDefault();   // stop page scrolling
});
```

`setDir(dx, dy)` ignores the input if it would reverse direction (e.g., moving right → left).

## Score & High Score

- `score` increments by 10 per food eaten.
- `highScore` stored in `localStorage` under key `snakeHighScore`.
- Both displayed in the Bootstrap navbar badge, updated via jQuery each tick.

## Button Behavior

| Button | `state` transitions |
|--------|---------------------|
| Start | `idle` → `playing` |
| Pause | `playing` ↔ `paused` (toggles label) |
| Restart | any → resets all state → `playing` |

Buttons are enabled/disabled via Bootstrap's `disabled` class based on current `state`.

## Game Over

1. Stop the interval.
2. `state = 'gameover'`.
3. Draw a semi-transparent overlay on canvas with "GAME OVER" and final score.
4. Update high score if beaten.
5. Enable Restart button.

## Visual Style

| Element | Color |
|---------|-------|
| Canvas background | `#1a1a2e` (dark navy) |
| Grid lines | `#16213e` |
| Snake body | `#4ade80` (green-400) |
| Snake head | `#86efac` (green-300, brighter) |
| Food | `#f87171` (red-400) |
| Overlay text | white |
| Page background | Bootstrap `bg-dark` |
| Text | Bootstrap `text-light` |

## Sequence Diagram

```
User presses Start
  → jQuery click → startGame() → state='playing' → startLoop()
  → setInterval fires → tick() → draw() → updateScore()
  → ...
User presses arrow key
  → jQuery keydown → setDir() → nextDir updated
  → next tick uses nextDir
Snake eats food
  → score++ → placeFood() → maybe adjustSpeed()
Snake hits wall/self
  → gameOver() → clearInterval → draw overlay → update highScore
User presses Restart
  → resetState() → startGame()
```
