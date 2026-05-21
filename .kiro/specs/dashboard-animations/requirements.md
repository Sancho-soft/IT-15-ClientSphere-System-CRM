# Requirements Document

## Introduction

ClientSphere is an ASP.NET Core CRM application with four distinct layouts: a public landing layout (`_Layout.cshtml`), an admin layout (`_AdminLayout.cshtml`), a CRM staff layout (`_CrmLayout.cshtml`), and a customer portal layout (`_CustomerLayout.cshtml`). GSAP 3.12.5 with ScrollTrigger is already loaded on all four layouts, and `site.js` already defines handlers for the animation classes `.gsap-fade-in`, `.gsap-slide-up`, `.gsap-stagger-container`/`.gsap-stagger-item`, and `.gsap-pulse`. Currently only the landing page makes meaningful use of these classes; the admin, CRM, and customer portal dashboards load GSAP but apply almost none of the animation classes to their content.

This feature enhances the UI by applying the existing GSAP animation infrastructure to dashboard stat cards, data tables, list groups, section headers, and action buttons across all authenticated layouts so the application feels polished and alive — without introducing new JavaScript libraries or breaking existing functionality.

## Glossary

- **Animation_System**: The combination of GSAP 3.12.5, ScrollTrigger, and the CSS/JS handlers already defined in `site.js` and `site.css`.
- **Dashboard_View**: Any Razor view that renders a role-specific overview page (e.g., `Admin/Dashboard.cshtml`, `SalesManager/Dashboard.cshtml`, `SalesStaff/Dashboard.cshtml`, `SupportStaff/Dashboard.cshtml`, `MarketingStaff/Dashboard.cshtml`, `MarketingManager/Dashboard.cshtml`, `BillingStaff/Dashboard.cshtml`, `CustomerPortal/Dashboard.cshtml`).
- **Stat_Card**: A Bootstrap `.card` element used to display a single KPI metric (e.g., Total Users, Revenue, Open Tickets).
- **Data_Table**: An HTML `<table>` element used to display tabular records such as sales orders, invoices, tickets, or leads.
- **Section_Header**: An `<h4>`, `<h5>`, or `<h6>` element that introduces a content section within a Dashboard_View.
- **Action_Button**: A primary call-to-action button or link-button rendered within a Dashboard_View (e.g., "View All Sales", "Create Ticket", "New Order").
- **Stagger_Group**: A container element with class `gsap-stagger-container` whose direct children carry class `gsap-stagger-item`, triggering a cascading entrance animation via the existing `site.js` handler.
- **Entrance_Animation**: A one-time play-on-scroll animation (fade-in or slide-up) triggered when an element enters the viewport, using the existing ScrollTrigger configuration in `site.js`.
- **Hover_Animation**: A scale pulse triggered on `mouseenter`/`mouseleave` via the existing `.gsap-pulse` handler in `site.js`.
- **Reduced_Motion**: The `prefers-reduced-motion: reduce` CSS media query, used to respect user accessibility preferences.

---

## Requirements

### Requirement 1: Stat Card Entrance Animations on Dashboard Views

**User Story:** As a CRM staff member or administrator, I want the KPI stat cards on my dashboard to animate into view when the page loads, so that the interface feels dynamic and draws my attention to key metrics.

#### Acceptance Criteria

1. WHEN a Dashboard_View is rendered, THE Animation_System SHALL apply an Entrance_Animation to each Stat_Card row using the `gsap-stagger-container` and `gsap-stagger-item` classes so that cards cascade in with a stagger of 0.1 seconds.
2. WHEN a Stat_Card row contains four or fewer cards, THE Animation_System SHALL complete the full stagger sequence within 0.7 seconds of the trigger firing.
3. IF a Stat_Card is already visible in the viewport on page load without scrolling, THEN THE Animation_System SHALL still play the Entrance_Animation for that card.
4. WHERE the `gsap-stagger-container` class is present on a row element, THE Animation_System SHALL treat every direct child `.col-*` element that wraps a Stat_Card as a `gsap-stagger-item`.

---

### Requirement 2: Section Header Entrance Animations

**User Story:** As a user navigating a dashboard, I want section headings to slide into view as I scroll, so that content sections feel structured and visually separated.

#### Acceptance Criteria

1. WHEN a Section_Header enters the viewport, THE Animation_System SHALL apply the `gsap-slide-up` Entrance_Animation to that element.
2. THE Animation_System SHALL animate each Section_Header independently so that multiple headers on the same page do not animate simultaneously.
3. IF a Section_Header is inside a card header element (`.card-header`), THEN THE Animation_System SHALL animate the entire `.card-header` element rather than the heading tag alone.

---

### Requirement 3: Data Table Row Entrance Animations

**User Story:** As a staff member viewing a data table on a dashboard, I want table rows to animate in when the table scrolls into view, so that the data presentation feels intentional rather than static.

#### Acceptance Criteria

1. WHEN a Data_Table enters the viewport, THE Animation_System SHALL apply a stagger Entrance_Animation to the `<tbody>` rows using the `gsap-stagger-container` and `gsap-stagger-item` classes.
2. THE Animation_System SHALL stagger Data_Table row animations with a delay of 0.05 seconds per row so that the cascade is visible but does not feel slow.
3. IF a Data_Table contains more than 20 rows, THEN THE Animation_System SHALL cap the stagger animation to the first 20 visible rows to prevent performance degradation.
4. WHILE a Data_Table is being populated via an AJAX or partial-view reload, THE Animation_System SHALL not re-trigger the Entrance_Animation on rows that were already animated.

---

### Requirement 4: Action Button Hover Animations

**User Story:** As a user interacting with dashboard action buttons, I want buttons to respond visually to hover, so that interactive elements are clearly distinguishable.

#### Acceptance Criteria

1. WHEN a user hovers over an Action_Button that carries the `gsap-pulse` class, THE Animation_System SHALL scale the element to 1.05 over 0.2 seconds.
2. WHEN a user moves the cursor away from a `gsap-pulse` Action_Button, THE Animation_System SHALL return the element to scale 1.0 over 0.2 seconds.
3. THE Animation_System SHALL apply the `gsap-pulse` class to primary call-to-action buttons on each Dashboard_View (e.g., "View All", "Create", "Export" buttons).
4. IF a button already carries the `hover-lift` CSS class, THEN THE Animation_System SHALL NOT also apply `gsap-pulse` to that same element to avoid conflicting transform effects.

---

### Requirement 5: Consistent Animation Behaviour Across All Four Layouts

**User Story:** As a product owner, I want animations to behave consistently across the Admin, CRM, and Customer Portal layouts, so that the application has a unified visual identity regardless of which role a user holds.

#### Acceptance Criteria

1. THE Animation_System SHALL apply Entrance_Animations to Dashboard_Views rendered under `_AdminLayout.cshtml`, `_CrmLayout.cshtml`, and `_CustomerLayout.cshtml` using the same GSAP class conventions.
2. WHEN a Dashboard_View is rendered under any of the three authenticated layouts, THE Animation_System SHALL animate the page header (already carrying `gsap-fade-in`) and the main content wrapper (already carrying `gsap-slide-up`) as defined in the existing layout files without modification to the layout files themselves.
3. THE Animation_System SHALL NOT introduce new JavaScript files or new GSAP plugins beyond those already loaded in the layouts.
4. IF a view uses a partial view or a ViewComponent to render a section, THEN THE Animation_System SHALL apply animation classes within that partial or component so that the section animates correctly when included in any layout.

---

### Requirement 6: Accessibility — Reduced Motion Support

**User Story:** As a user who has enabled the "reduce motion" accessibility setting on their operating system, I want animations to be suppressed, so that I am not affected by motion that could cause discomfort.

#### Acceptance Criteria

1. WHILE the `prefers-reduced-motion: reduce` media query is active, THE Animation_System SHALL set the duration of all GSAP Entrance_Animations to 0 seconds so that elements appear instantly without motion.
2. WHILE the `prefers-reduced-motion: reduce` media query is active, THE Animation_System SHALL disable the Hover_Animation scale effect on `gsap-pulse` elements.
3. THE Animation_System SHALL implement Reduced_Motion support in `site.js` by reading `window.matchMedia('(prefers-reduced-motion: reduce)')` before registering GSAP animations.
4. IF the user changes their motion preference after the page has loaded, THEN THE Animation_System SHALL NOT retroactively alter already-completed animations, but SHALL respect the updated preference for any animations not yet triggered.

---

### Requirement 7: Animation Performance — No Layout Thrashing

**User Story:** As a user on a mid-range device, I want dashboard animations to run at 60 fps without causing visible jank or layout reflow, so that the application remains responsive.

#### Acceptance Criteria

1. THE Animation_System SHALL animate only CSS `transform` and `opacity` properties in all GSAP Entrance_Animations and Hover_Animations to avoid triggering browser layout recalculation.
2. THE Animation_System SHALL NOT animate CSS properties that trigger layout reflow such as `width`, `height`, `margin`, `padding`, or `top`/`left` positional properties.
3. WHEN ScrollTrigger initialises on a Dashboard_View, THE Animation_System SHALL call `ScrollTrigger.refresh()` only once after all animations on the page have been registered, not once per element.
4. IF a Dashboard_View contains more than 50 animated elements, THEN THE Animation_System SHALL batch-register those elements using `gsap.utils.toArray()` rather than registering each element individually in a loop.
