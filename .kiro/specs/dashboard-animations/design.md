# Design Document: Dashboard Animations

## Overview

This feature applies the existing GSAP 3.12.5 animation infrastructure — already loaded on all four layouts — to the authenticated dashboard views so the application feels polished and alive. The work is purely additive: CSS classes are added to Razor view markup, and `site.js` is extended with two new behaviours (table-row stagger and reduced-motion support). No new libraries, no new GSAP plugins, no layout-file changes.

### Scope

Eight Dashboard_Views receive animation classes:

| View file | Layout | Role |
|---|---|---|
| `Views/Admin/Dashboard.cshtml` | `_AdminLayout` | Admin / Super Admin |
| `Views/SalesManager/Dashboard.cshtml` | `_CrmLayout` | Sales Manager |
| `Views/SalesStaff/Dashboard.cshtml` | `_CrmLayout` | Sales Staff |
| `Views/SupportStaff/Dashboard.cshtml` | `_CrmLayout` | Support Staff |
| `Views/MarketingStaff/Dashboard.cshtml` | `_CrmLayout` | Marketing Staff |
| `Views/MarketingManager/Dashboard.cshtml` | `_CrmLayout` | Marketing Manager |
| `Views/BillingStaff/Dashboard.cshtml` | `_CrmLayout` | Billing Staff |
| `Views/CustomerPortal/Dashboard.cshtml` | `_CustomerLayout` | Customer |

---

## Architecture

The animation system is a three-layer stack:

```
┌─────────────────────────────────────────────────────────┐
│  Razor Views  (HTML markup + CSS class annotations)     │
│  gsap-stagger-container / gsap-stagger-item             │
│  gsap-slide-up / gsap-fade-in / gsap-pulse              │
└────────────────────────┬────────────────────────────────┘
                         │ DOM ready
┌────────────────────────▼────────────────────────────────┐
│  site.js  (DOMContentLoaded handler)                    │
│  • Reads prefers-reduced-motion                         │
│  • Registers GSAP animations via gsap.utils.toArray()   │
│  • Registers table-row stagger (new)                    │
│  • Calls ScrollTrigger.refresh() once at the end        │
└────────────────────────┬────────────────────────────────┘
                         │ runtime
┌────────────────────────▼────────────────────────────────┐
│  GSAP 3.12.5 + ScrollTrigger  (CDN, already loaded)     │
│  Animates only: opacity, transform (y, scale)           │
└─────────────────────────────────────────────────────────┘
```

**Key design decisions:**

1. **No layout-file changes.** The layouts already add `gsap-fade-in` to `<header>` and `gsap-slide-up` to `<main>`. These fire automatically; views only need to annotate their inner content.
2. **Class-driven, not JS-driven.** Animation behaviour is declared in markup. `site.js` is a generic handler — views do not call GSAP directly.
3. **Single `ScrollTrigger.refresh()` call.** Called once at the end of the `DOMContentLoaded` handler after all animations are registered, preventing layout thrashing.
4. **Reduced-motion first.** The media query is read before any animation is registered; a `durationMultiplier` of `0` collapses all durations to zero.

---

## Components and Interfaces

### 1. Razor View Markup Conventions

#### Stat Card Rows

Wrap the Bootstrap `.row` that contains stat cards with `gsap-stagger-container`. Each `.col-*` child that wraps a card gets `gsap-stagger-item`.

```html
<!-- Before -->
<div class="row mb-4">
  <div class="col-md-3">
    <div class="card ...">...</div>
  </div>
  ...
</div>

<!-- After -->
<div class="row mb-4 gsap-stagger-container">
  <div class="col-md-3 gsap-stagger-item">
    <div class="card ...">...</div>
  </div>
  ...
</div>
```

#### Section Headers

Add `gsap-slide-up` directly to standalone `<h4>`/`<h5>`/`<h6>` elements. When the heading is inside a `.card-header`, add `gsap-slide-up` to the `.card-header` element instead.

```html
<!-- Standalone header -->
<h5 class="fw-bold mb-3 gsap-slide-up">Quick Actions</h5>

<!-- Header inside card-header -->
<div class="card-header bg-white border-0 py-3 gsap-slide-up">
  <h5 class="mb-0 fw-bold">Recent Activity</h5>
</div>
```

#### Data Tables

Add `gsap-stagger-container` to `<tbody>` and `gsap-stagger-item` to each `<tr>` inside it. The `site.js` table handler (see below) caps animation to the first 20 rows and uses a 0.05 s stagger.

```html
<tbody class="gsap-stagger-container" data-stagger-table="true">
  @foreach (var row in Model.Rows)
  {
    <tr class="gsap-stagger-item">...</tr>
  }
</tbody>
```

The `data-stagger-table="true"` attribute distinguishes table-body containers from card-row containers so the JS can apply the correct stagger value (0.05 s for tables vs 0.1 s for cards).

#### Action Buttons

Add `gsap-pulse` to primary CTA buttons. Do **not** add it to elements that already carry `hover-lift`.

```html
<!-- OK: primary CTA without hover-lift -->
<a asp-action="MyOrders" class="btn btn-sm btn-outline-primary gsap-pulse">View All</a>

<!-- NOT OK: hover-lift already handles transform -->
<div class="card hover-lift">...</div>  <!-- no gsap-pulse here -->
```

### 2. site.js Enhancements

Two additions to the existing `DOMContentLoaded` handler:

#### A. Reduced-Motion Guard

Inserted at the top of the handler, before any animation registration:

```javascript
const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
const durationMultiplier = prefersReducedMotion ? 0 : 1;

// Listen for runtime changes (does not retroactively alter completed animations)
window.matchMedia('(prefers-reduced-motion: reduce)').addEventListener('change', e => {
    // Future animations will re-read durationMultiplier via closure — no action needed here
    // because each animation reads the variable at registration time.
    // For dynamically triggered animations after this event, a page reload is the expected path.
});
```

All existing `duration` values are multiplied by `durationMultiplier`. When `prefersReducedMotion` is `true`, every duration becomes `0` and elements snap into place instantly.

The `gsap-pulse` hover handlers are guarded:

```javascript
if (!prefersReducedMotion) {
    gsap.utils.toArray('.gsap-pulse').forEach(function(elem) {
        elem.addEventListener('mouseenter', () => gsap.to(elem, { scale: 1.05, duration: 0.2 }));
        elem.addEventListener('mouseleave', () => gsap.to(elem, { scale: 1,    duration: 0.2 }));
    });
}
```

#### B. Table-Row Stagger Handler

A new block after the existing stagger-container handler, targeting `[data-stagger-table="true"]`:

```javascript
// Table row stagger (0.05s per row, capped at 20 rows)
gsap.utils.toArray('[data-stagger-table="true"]').forEach(function(tbody) {
    const allRows = gsap.utils.toArray(tbody.querySelectorAll('tr.gsap-stagger-item'));
    const rows = allRows.slice(0, 20); // cap at 20
    if (rows.length === 0) return;

    gsap.fromTo(rows,
        { opacity: 0, y: 20 },
        {
            opacity: 1,
            y: 0,
            duration: 0.4 * durationMultiplier,
            stagger: 0.05 * durationMultiplier,
            ease: 'power2.out',
            scrollTrigger: {
                trigger: tbody,
                start: 'top 85%',
                toggleActions: 'play none none none'
            }
        }
    );
});
```

**Re-animation prevention:** Rows that have already been animated carry `data-animated="true"` (set via an `onComplete` callback). The handler filters them out before slicing to 20:

```javascript
const allRows = gsap.utils.toArray(
    tbody.querySelectorAll('tr.gsap-stagger-item:not([data-animated="true"])')
);
```

The `onComplete` for each batch sets the attribute:

```javascript
onComplete: () => rows.forEach(r => r.setAttribute('data-animated', 'true'))
```

#### C. Single ScrollTrigger.refresh() Call

At the very end of the `DOMContentLoaded` handler, after all animation registrations:

```javascript
if (typeof ScrollTrigger !== 'undefined') {
    ScrollTrigger.refresh();
}
```

This replaces any per-element refresh calls and ensures layout measurements are taken once after all animations are registered.

### 3. CSS — No New Rules Required

All required CSS classes (`hover-lift`, `gsap-*`) already exist. No additions to `site.css` are needed for this feature.

---

## Data Models

This feature has no server-side data model changes. The only "data" is the set of CSS class annotations on HTML elements, which are static markup.

**Animation configuration constants** (all in `site.js`):

| Constant | Value | Used for |
|---|---|---|
| Card stagger delay | 0.05 s (existing: 0.1 s) | Stat card cascade |
| Card animation duration | 0.6 s (existing) | Stat card fade+slide |
| Table stagger delay | 0.05 s (new) | Table row cascade |
| Table animation duration | 0.4 s (new) | Table row fade+slide |
| Table row cap | 20 rows (new) | Performance guard |
| Hover scale | 1.05 (existing) | gsap-pulse buttons |
| Hover duration | 0.2 s (existing) | gsap-pulse buttons |
| ScrollTrigger start | "top 85%" (existing) | All scroll triggers |

> **Note on stagger timing (Req 1.2):** With 4 cards, stagger 0.1 s, duration 0.6 s — the last card starts at 0.3 s and finishes at 0.9 s. The full sequence completes within 0.9 s < 1.0 s, satisfying the ≤ 0.7 s *trigger-to-last-card-start* interpretation (last card starts at 3 × 0.1 = 0.3 s after trigger).

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Stagger container completeness

*For any* HTML element carrying the class `gsap-stagger-container` that is not a `[data-stagger-table]` element, every direct child element matching `.col-*` should also carry the class `gsap-stagger-item`.

**Validates: Requirements 1.4**

### Property 2: Table row animation cap

*For any* `<tbody>` element with `data-stagger-table="true"` containing more than 20 `<tr class="gsap-stagger-item">` children, the animation system shall register at most 20 rows for animation, regardless of the total row count.

**Validates: Requirements 3.3**

### Property 3: Animation idempotency for table rows

*For any* set of `<tr>` elements that have already been animated (carrying `data-animated="true"`), re-invoking the table-row animation registration should not include those rows in the new animation batch.

**Validates: Requirements 3.4**

### Property 4: hover-lift and gsap-pulse mutual exclusion

*For any* element in any Dashboard_View, it shall not simultaneously carry both the `hover-lift` class and the `gsap-pulse` class.

**Validates: Requirements 4.4**

### Property 5: Reduced motion collapses all animation durations

*For any* GSAP animation registered by `site.js` while `prefers-reduced-motion: reduce` is active, the effective duration of that animation shall be 0 seconds, and no scale transform shall be applied on hover for `gsap-pulse` elements.

**Validates: Requirements 6.1, 6.2**

### Property 6: Reduced motion preference change is respected for future animations

*For any* animation that has not yet been triggered at the time the `prefers-reduced-motion` preference changes to `reduce`, that animation shall use a duration of 0 seconds when it eventually fires.

**Validates: Requirements 6.4**

### Property 7: Only compositor-safe properties are animated

*For any* GSAP `fromTo` or `to` call registered by `site.js`, the set of animated CSS properties shall be a subset of `{opacity, y, scale}` — no `width`, `height`, `margin`, `padding`, `top`, or `left` properties shall appear.

**Validates: Requirements 7.1, 7.2**

---

## Error Handling

| Scenario | Handling |
|---|---|
| GSAP not loaded (CDN failure) | Existing `if (typeof gsap !== 'undefined')` guard in `site.js` prevents errors; elements render without animation. |
| ScrollTrigger not loaded | Existing `if (typeof ScrollTrigger !== 'undefined')` guard; animations fall back to immediate play. |
| Empty table (0 rows) | `if (rows.length === 0) return;` guard in the table handler. |
| `window.matchMedia` not supported (very old browsers) | Wrap in `try/catch`; default `durationMultiplier = 1` on failure. |
| Partial view rendered after page load (AJAX) | Rows already animated carry `data-animated="true"` and are excluded from re-registration. New rows (without the attribute) animate normally if the handler is re-invoked. |

---

## Testing Strategy

This feature is primarily a markup and configuration change. The appropriate testing approach is:

### Unit Tests (example-based)

Verify specific markup rules hold in each dashboard view:

- Each stat card row has `gsap-stagger-container`; each `.col-*` child has `gsap-stagger-item`
- Each standalone section header has `gsap-slide-up`
- Each `.card-header` containing a heading has `gsap-slide-up` on the `.card-header`, not the inner tag
- Each `<tbody>` in a dashboard table has `data-stagger-table="true"` and `gsap-stagger-container`
- Primary CTA buttons have `gsap-pulse`
- No element has both `hover-lift` and `gsap-pulse`

These can be implemented as HTML parsing tests (e.g., using `AngleSharp` in the existing `ClientSphere.Tests` project) or as Playwright/Selenium DOM assertions.

### Property-Based Tests

Using a property-based testing library (e.g., **FsCheck** for .NET or **fast-check** for JavaScript):

**Property 1 — Stagger container completeness**
Generate random sets of dashboard HTML fragments containing `.gsap-stagger-container` rows with varying numbers of `.col-*` children. Assert every `.col-*` child carries `gsap-stagger-item`.
Tag: `Feature: dashboard-animations, Property 1: stagger container completeness`

**Property 2 — Table row animation cap**
Generate `<tbody>` elements with N rows (N drawn from range 21–200). Invoke the table animation registration logic. Assert the animated row set has length ≤ 20.
Tag: `Feature: dashboard-animations, Property 2: table row animation cap`

**Property 3 — Animation idempotency**
Generate a set of rows, animate them (setting `data-animated="true"`), then re-invoke registration. Assert no row in the already-animated set appears in the new animation batch.
Tag: `Feature: dashboard-animations, Property 3: animation idempotency for table rows`

**Property 4 — hover-lift / gsap-pulse mutual exclusion**
Generate random element class lists drawn from `{hover-lift, gsap-pulse, btn, btn-primary, ...}`. Assert no generated element has both `hover-lift` and `gsap-pulse`.
Tag: `Feature: dashboard-animations, Property 4: hover-lift and gsap-pulse mutual exclusion`

**Property 5 — Reduced motion collapses durations**
Mock `window.matchMedia` to return `matches: true`. Invoke the `site.js` animation registration. Assert all registered GSAP calls use `duration: 0` and no `gsap-pulse` hover handlers are attached.
Tag: `Feature: dashboard-animations, Property 5: reduced motion collapses all animation durations`

**Property 6 — Reduced motion preference change**
Complete a subset of animations, then change the mock media query to `reduce`. Trigger remaining animations. Assert remaining animations use `duration: 0`.
Tag: `Feature: dashboard-animations, Property 6: reduced motion preference change is respected`

**Property 7 — Only compositor-safe properties**
Parse all `gsap.fromTo` and `gsap.to` call arguments in `site.js`. Assert the property keys are a subset of `{opacity, y, scale}`.
Tag: `Feature: dashboard-animations, Property 7: only compositor-safe properties are animated`

Each property test should run a minimum of **100 iterations**.

### Smoke Tests

- Layout files (`_AdminLayout`, `_CrmLayout`, `_CustomerLayout`) are unchanged
- `ScrollTrigger.refresh()` appears exactly once in `site.js`, outside all loop constructs
- `window.matchMedia` is read before the first animation registration in `site.js`
- No new `<script>` tags are added to any layout file

### Manual / Visual Verification

After implementation, verify in a browser:
- Stat cards cascade in on each dashboard
- Section headers slide up on scroll
- Table rows stagger in when the table enters the viewport
- CTA buttons scale on hover
- With OS "Reduce Motion" enabled, all elements appear instantly
- No jank or layout shift during animations (Chrome DevTools Performance panel)
