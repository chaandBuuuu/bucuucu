# Devour 2D - Complete Ability Reference

## 🔴 HUNT #1 - Root Master

**Role**: Hunter | **Speed**: 4.5 | **Health**: 100

### Passive: Root Trails
- Leaves roots when moving (duration: X seconds)
- Hunt #1 on root: **+40% speed**
- Survivor on root: **-30% speed (Slowness)**

### E Ability: Vine Pull
- **Range**: 5m
- **Effect**: Pulls survivors toward Hunt #1
- **Cooldown**: 8s
- **Cost**: None

### R Ability: Flower Bloom
- **Range**: 3m radius
- **Effect**: Creates bloom at position
  - Survivors in range get **Slowness X (0.3)** + **True Sight** for 5s
- **Cooldown**: 10s

### F Ability: Dash Forward
- **Distance**: 5m
- **Duration**: 0.35s
- **If hits Survivor**: **20 damage** + continue dashing
- **If misses**: **Self Stun (2s)**
- **Cooldown**: 12s

---

## 🟡 HUNT #2 - Eyes/Spotlight

**Role**: Hunter | **Speed**: 5 | **Health**: 100

### Passive: Cone Vision
- Vision is limited to 45° cone ahead (8m range)
- Survivors in cone:
  - **After 3s**: Slowness (0.4)
  - **Continuous**: 5 DPS

### E Ability: Light Flash
- **Effect**: Expands vision cone to 80° for 4s
- **Detection**: Reveals all survivors in expanded range
- **All revealed get**: **True Sight (5s)**
- **Cooldown**: 7s

### R Ability: Narrow Beam
- **Only works**: When survivor visible in cone
- **Effect**: Cone narrows to laser line over 2s
- **Then**: Fire stun beam (10m range)
  - Hit survivor: **Stun (2s)** + **True Sight**
  - Hunt #2 gets: **Blindness (3s)**
- **Cooldown**: 15s

### F Ability: Light Orbs
- **Max Spheres**: 3
- **Range per orb**: 5m vision
- **Effect**: Survivors near orb get **Slowness (0.3)**
- **Orb Duration**: 10s
- **If destroyed**: **Hunt #2 Stun (1.5s)**
- **Cooldown**: 6s per orb

---

## 🟢 SURVIVAL #1 - Marksman

**Role**: Survivor | **Speed**: 5.5 | **Health**: 80

### Passive: Ammo System
- Starts with **6 Mark Rounds**
- 3 Mark Rounds → 1 Tiger Round
- Below 50% HP: **Captain Black (8s)**
  - Clears debuffs
  - -30% damage taken
- No ammo: E & R locked

### E Ability: Swing
- **Damage**: 25
- **Range**: 2m
- **With Mark Round**:
  - Target Hunter: **Stun (1.5s)**
  - Uses 1 Mark Round
- **With Tiger Round**:
  - Target Hunter: **Stun (2s)** + **Knockback(5)**
  - Uses 1 Tiger Round
- **Cooldown**: 1s

### R Ability: Reload
- **Effect**: Switches between Mark ↔ Tiger rounds
- **During reload**: **Slowness (0.5)** for 2s
- **Cooldown**: 3s

---

## 🔵 SURVIVAL #2 - Boombox Player

**Role**: Survivor | **Speed**: 5.2 | **Health**: 85

### Passive: Movement Bonus
- **Moving away**: Gets **Swiftness (0.2)**
- **Hunter nearby (5m)**: Gets **Slowness (0.3)** debuff

### E Ability: Place Boombox
- **Range**: 4m sound radius
- **Duration**: 8s
- **Effect**: In boombox area:
  - Survivors: **+Swiftness (0.3)**
  - Hunters: **+Slowness (0.3)**
- **Cooldown**: 10s
- **Max 1 active**

### R Ability: Clap
- **Range**: 3m radius (90° cone)
- **Self**: **Stun (1s)**
- **Hit Hunter**: **Stun (1.5s)**
- **Hit Survivor**: **Knockback (3)**
- **Cooldown**: 8s

---

## 🟠 SURVIVAL #3 - Lumberjack

**Role**: Survivor | **Speed**: 5 | **Health**: 90

### Passive: Wood Carrier
- **Holding Wood**: **+40% speed (Swiftness 0.4)**

### E Ability: Wood Detect
- **Range**: 8m
- **Effect**: Shows all nearby wood
- **Visual**: Highlights wood pickups
- **Cooldown**: 3s

### R Ability: Wood Throw
- **Only when**: Holding wood
- **Range**: 7m
- **Target**: Hunter in range
- **Effect**: **Stun (1.5s)**
- **Cost**: 1 wood
- **Cooldown**: 2s

---

## 🟣 SURVIVAL #4 - Support

**Role**: Survivor | **Speed**: 5 | **Health**: 75

### Passive: Bonfire Aura
- **Near bonfire (3m)**: **Slowness (-0.2)** debuff

### E Ability: Support Swing
- **Range**: 2m
- **Target**: Hunter
- **Effect**: **Stun (1s)**
- **Cooldown**: 2s

### R Ability: Tap Ground
- **Range**: 4m aura
- **Effect**: All survivors in range get:
  - **Swiftness (0.3)** for 3s
- **Visual**: Ground shake/pulse effect
- **Cooldown**: 5s

---

## 📊 Status Effects Reference

| Effect | Duration | Magnitude | Effect |
|--------|----------|-----------|--------|
| **Slowness** | Custom | 0.2-0.5 | Reduces move speed |
| **Stun** | 1-2s | N/A | Cannot move |
| **Swiftness** | 1-3s | 0.2-0.4 | Increases move speed |
| **True Sight** | 3-5s | N/A | Revealed location |
| **Blindness** | 3s | N/A | Reduced vision |
| **Captain Black** | 8s | 0.3 | 30% damage reduction + debuff clear |
| **Burn** | Variable | Variable | Continuous damage |

---

## 🎲 Balance Notes

- **Hunter Advantage**: Raw damage, mobility, crowd control
- **Survivor Advantage**: Numbers (3v1), teamwork, escape objective
- **Cooldowns**: Average 5-10s for balance
- **Health**: Hunter = Survivor base (100 vs avg 85)
- **Speed**: Mostly same, varied by abilities

---

**Version**: 1.0 | **Last Updated**: 2025-04-07
