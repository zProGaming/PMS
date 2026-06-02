document.addEventListener("DOMContentLoaded", () => {
  const backButton = document.querySelector("[data-vpms-back-button]");
  if (backButton) {
    backButton.addEventListener("click", () => {
      if (window.history.length > 1) {
        window.history.back();
        return;
      }

      window.location.href = "/";
    });
  }

  const confirmDialog = (() => {
    if (!window.bootstrap) {
      return null;
    }

    const modal = document.createElement("div");
    modal.className = "modal fade vpms-command-modal vpms-confirm-dialog";
    modal.id = "vpmsConfirmDialog";
    modal.tabIndex = -1;
    modal.setAttribute("aria-labelledby", "vpmsConfirmDialogTitle");
    modal.setAttribute("aria-hidden", "true");
    modal.innerHTML = `
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <div>
              <span class="page-kicker">Workflow Confirmation</span>
              <h2 class="modal-title" id="vpmsConfirmDialogTitle">Confirm Action</h2>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <p class="vpms-confirm-message mb-0"></p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn vpms-btn-secondary" data-bs-dismiss="modal">Keep Reviewing</button>
            <button type="button" class="btn vpms-btn-primary" data-vpms-confirm-continue>Continue</button>
          </div>
        </div>
      </div>`;
    document.body.appendChild(modal);

    const instance = bootstrap.Modal.getOrCreateInstance(modal);
    const messageElement = modal.querySelector(".vpms-confirm-message");
    const continueButton = modal.querySelector("[data-vpms-confirm-continue]");
    let pendingTrigger = null;

    continueButton?.addEventListener("click", () => {
      const trigger = pendingTrigger;
      pendingTrigger = null;
      instance.hide();

      if (!trigger) {
        return;
      }

      trigger.dataset.vpmsConfirmApproved = "1";
      const form = trigger.form instanceof HTMLFormElement ? trigger.form : trigger.closest("form");
      const isSubmitControl = trigger.matches("button[type='submit'], input[type='submit'], button:not([type]), input:not([type])");

      if (form && isSubmitControl && typeof form.requestSubmit === "function") {
        form.requestSubmit(trigger);
        return;
      }

      trigger.click();
    });

    modal.addEventListener("hidden.bs.modal", () => {
      pendingTrigger = null;
    });

    return {
      open(trigger, message) {
        pendingTrigger = trigger;
        if (messageElement) {
          messageElement.textContent = message;
        }

        instance.show();
      }
    };
  })();

  document.addEventListener("click", (event) => {
    const trigger = event.target?.closest?.("[data-vpms-confirm]");
    if (!trigger) {
      return;
    }

    const message = trigger.getAttribute("data-vpms-confirm");
    if (!message) {
      return;
    }

    if (trigger.dataset.vpmsConfirmApproved === "1") {
      delete trigger.dataset.vpmsConfirmApproved;
      return;
    }

    event.preventDefault();

    if (!confirmDialog) {
      if (window.confirm(message)) {
        trigger.dataset.vpmsConfirmApproved = "1";
        trigger.click();
      }

      return;
    }

    confirmDialog.open(trigger, message);
  });

  const navIconMap = {
    "dashboard": "home",
    "executive dashboard": "chart",
    "front office": "desk",
    "front office dashboard": "desk",
    "reservations": "calendar",
    "guests": "people",
    "room rack": "rooms",
    "room readiness board": "sparkle",
    "arrivals": "calendar",
    "departures": "calendar",
    "in-house guests": "people",
    "booking requests": "globe",
    "guest portal": "portal",
    "group bookings": "calendar",
    "housekeeping": "sparkle",
    "room status": "rooms",
    "housekeeping tasks": "check",
    "maintenance": "tool",
    "service requests": "request",
    "finance": "finance",
    "finance dashboard": "finance",
    "folios": "ledger",
    "payments": "cash",
    "night audit": "moon",
    "cashier shifts": "cash",
    "finance documents": "document",
    "documents": "document",
    "refunds": "cash",
    "void requests": "alert",
    "discount approvals": "check",
    "accounts receivable": "ledger",
    "ar invoices": "invoice",
    "ar aging": "clock",
    "accounts payable": "ledger",
    "payment vouchers": "document",
    "group folios": "ledger",
    "group collections": "cash",
    "pseudo room folios": "ledger",
    "bank reconciliation": "ledger",
    "month-end close": "check",
    "accounting dashboard": "ledger",
    "chart of accounts": "ledger",
    "journal entries": "document",
    "posting batches": "inbox",
    "general ledger": "ledger",
    "trial balance": "report",
    "balance sheet": "report",
    "profit and loss": "chart",
    "statement of cash flows": "cash",
    "usali operating statement": "report",
    "philippine reports": "report",
    "report center": "report",
    "f&b service": "food",
    "pos dashboard": "receipt",
    "new order": "plus",
    "orders": "receipt",
    "outlets": "building",
    "tables": "banquet",
    "menu items": "menu",
    "f&b kitchen": "kitchen",
    "kitchen display": "display",
    "kitchen stations": "station",
    "sales": "sales",
    "banquet": "banquet",
    "banquet dashboard": "banquet",
    "banquet events": "calendar",
    "beos": "document",
    "event calendar": "calendar",
    "revenue": "chart",
    "revenue dashboard": "chart",
    "revenue calendar": "calendar",
    "rate plans": "document",
    "seasonal rates": "calendar",
    "restrictions": "alert",
    "inventory controls": "rooms",
    "promotion codes": "sales",
    "booking engine": "globe",
    "inventory": "box",
    "inventory dashboard": "box",
    "inventory items": "box",
    "stock movements": "inbox",
    "stock issue": "inbox",
    "stock adjustments": "check",
    "inventory reports": "report",
    "purchasing": "cart",
    "suppliers": "supplier",
    "purchase requests": "document",
    "purchase orders": "document",
    "receiving": "inbox",
    "labor dashboard": "people",
    "employee cost profiles": "people",
    "payroll periods": "calendar",
    "payroll cost entries": "cash",
    "payroll allocation rules": "org",
    "department labor budgets": "chart",
    "service charge pools": "cash",
    "labor reports": "report",
    "payroll import": "inbox",
    "group dashboard": "building",
    "organizations": "org",
    "consolidated reports": "report",
    "management ai": "spark",
    "reports": "report",
    "printable documents": "document",
    "document template settings": "gear",
    "audit logs": "audit",
    "system health": "pulse",
    "qa checklist": "check",
    "module qa": "check",
    "admin": "shield",
    "properties": "building",
    "departments": "org",
    "rooms": "rooms",
    "users and roles": "people",
    "settings": "gear",
    "system settings": "gear",
    "demo setup": "play",
    "presentation mode": "screen",
    "client package": "screen",
    "workflow launcher": "play",
    "error logs": "alert",
    "notifications": "request",
    "data validation issues": "pulse",
    "pseudo rooms": "rooms",
    "charge routing rules": "route",
    "ai settings": "spark"
  };

  const commandIconMap = {
    "support": "request",
    "alerts": "request",
    "reports": "report",
    "room calendar": "calendar",
    "reservation": "plus",
    "in-house": "people",
    "quick desk": "desk",
    "rack": "rooms",
    "room readiness": "rooms",
    "front cash": "cash",
    "pos": "receipt",
    "management ai": "spark",
    "system health": "pulse"
  };

  const normalizeLabel = (value) => (value || "").replace(/\s+/g, " ").trim().toLowerCase();
  const iconPath = (name) => {
    const paths = {
      home: '<path d="M3.5 10.5 12 4l8.5 6.5"/><path d="M5.5 9.5V20h13V9.5"/><path d="M9.5 20v-6h5v6"/>',
      desk: '<rect x="4" y="6" width="16" height="11" rx="2"/><path d="M8 20h8"/><path d="M12 17v3"/>',
      calendar: '<rect x="4" y="5" width="16" height="15" rx="2"/><path d="M8 3v4M16 3v4M4 10h16"/>',
      people: '<circle cx="9" cy="8" r="3"/><path d="M3.5 19c.8-3 2.6-4.5 5.5-4.5S13.7 16 14.5 19"/><circle cx="16.5" cy="9.5" r="2.3"/><path d="M14.8 15.2c2.3.1 3.8 1.3 4.7 3.8"/>',
      rooms: '<rect x="4" y="5" width="6.5" height="6.5" rx="1.2"/><rect x="13.5" y="5" width="6.5" height="6.5" rx="1.2"/><rect x="4" y="14.5" width="6.5" height="5.5" rx="1.2"/><rect x="13.5" y="14.5" width="6.5" height="5.5" rx="1.2"/>',
      portal: '<rect x="5" y="4" width="14" height="16" rx="2"/><path d="M10 12h6M13 9l3 3-3 3"/>',
      sparkle: '<path d="M12 3l1.8 5 5 1.8-5 1.8-1.8 5-1.8-5-5-1.8 5-1.8L12 3z"/><path d="M18 15l.8 2.2L21 18l-2.2.8L18 21l-.8-2.2L15 18l2.2-.8L18 15z"/>',
      tool: '<path d="M14.8 5.2a4 4 0 0 0 4.9 4.9L10 19.8a2.2 2.2 0 0 1-3.1-3.1l9.7-9.7a4 4 0 0 0-1.8-1.8z"/>',
      request: '<path d="M5 5h14v10H8l-3 3V5z"/><path d="M9 9h6M9 12h4"/>',
      route: '<path d="M6 6h5a3 3 0 0 1 0 6H9a3 3 0 0 0 0 6h9"/><circle cx="6" cy="6" r="2"/><circle cx="18" cy="18" r="2"/>',
      finance: '<path d="M12 3v18"/><path d="M16.5 7.5c-.9-1-2.4-1.5-4.1-1.5-2.2 0-3.9 1-3.9 2.7 0 3.8 8 1.8 8 5.8 0 1.8-1.8 3.2-4.4 3.2-1.9 0-3.6-.6-4.8-1.8"/>',
      moon: '<path d="M19 14.5A7.5 7.5 0 0 1 9.5 5a7.5 7.5 0 1 0 9.5 9.5z"/>',
      cash: '<rect x="4" y="7" width="16" height="10" rx="2"/><circle cx="12" cy="12" r="2.4"/><path d="M7 10v4M17 10v4"/>',
      document: '<path d="M7 3h7l4 4v14H7z"/><path d="M14 3v5h5M9.5 12h5M9.5 15h5M9.5 18h3"/>',
      ledger: '<path d="M6 4h12v16H6z"/><path d="M9 4v16M12 8h4M12 12h4M12 16h4"/>',
      invoice: '<path d="M7 4h10v16l-2-1-2 1-2-1-2 1-2-1z"/><path d="M10 8h4M10 12h4M10 16h2"/>',
      clock: '<circle cx="12" cy="12" r="8"/><path d="M12 7v5l3 2"/>',
      food: '<path d="M7 4v16M5 4v5a2 2 0 0 0 4 0V4M15 4v16M15 4c2 1.5 3 3.5 3 6h-3"/>',
      plus: '<path d="M12 5v14M5 12h14"/>',
      receipt: '<path d="M7 4h10v16l-2-1-2 1-2-1-2 1-2-1z"/><path d="M9.5 8h5M9.5 12h5M9.5 16h3"/>',
      menu: '<path d="M5 7h14M5 12h14M5 17h14"/>',
      kitchen: '<path d="M8 5c0 2-1 2-1 4s1 2 1 4"/><path d="M12 5c0 2-1 2-1 4s1 2 1 4"/><path d="M16 5c0 2-1 2-1 4s1 2 1 4"/><path d="M5 16h14v4H5z"/>',
      display: '<rect x="4" y="5" width="16" height="11" rx="2"/><path d="M8 20h8M12 16v4"/>',
      station: '<path d="M5 9h14M7 9v10M17 9v10M8 5h8l1 4H7z"/>',
      sales: '<path d="M5 17l5-5 3 3 6-8"/><path d="M15 7h4v4"/>',
      banquet: '<path d="M4 10h16M6 10v8M18 10v8M8 6h8l2 4H6z"/>',
      chart: '<path d="M4 19h16"/><path d="M7 16v-4M12 16V8M17 16V5"/>',
      globe: '<circle cx="12" cy="12" r="8"/><path d="M4 12h16M12 4a12 12 0 0 1 0 16M12 4a12 12 0 0 0 0 16"/>',
      box: '<path d="m12 3 8 4-8 4-8-4 8-4z"/><path d="M4 7v10l8 4 8-4V7"/><path d="M12 11v10"/>',
      cart: '<path d="M4 5h2l2 10h9l2-7H7"/><circle cx="10" cy="19" r="1.5"/><circle cx="17" cy="19" r="1.5"/>',
      supplier: '<path d="M4 17h16"/><path d="M6 17V8h8v9M14 11h4l2 3v3"/><circle cx="8" cy="19" r="1.5"/><circle cx="17" cy="19" r="1.5"/>',
      inbox: '<path d="M5 4h14l2 10v6H3v-6L5 4z"/><path d="M3 14h6l1.5 2h3L15 14h6"/>',
      spark: '<path d="M12 3l1.6 5.4L19 10l-5.4 1.6L12 17l-1.6-5.4L5 10l5.4-1.6L12 3z"/><path d="M18 16v5M15.5 18.5h5"/>',
      report: '<path d="M6 4h12v16H6z"/><path d="M9 16V9M12 16v-4M15 16V7"/>',
      audit: '<path d="M6 4h12v16H6z"/><path d="m9 12 2 2 4-5"/>',
      pulse: '<path d="M4 12h4l2-5 4 10 2-5h4"/>',
      check: '<path d="m5 12 4 4L19 6"/><rect x="4" y="4" width="16" height="16" rx="2"/>',
      shield: '<path d="M12 3 19 6v6c0 4.4-2.7 7.2-7 9-4.3-1.8-7-4.6-7-9V6z"/><path d="m9 12 2 2 4-5"/>',
      building: '<path d="M5 20V4h14v16"/><path d="M8 8h2M14 8h2M8 12h2M14 12h2M8 16h2M14 16h2"/>',
      org: '<rect x="9" y="4" width="6" height="4" rx="1"/><rect x="4" y="16" width="6" height="4" rx="1"/><rect x="14" y="16" width="6" height="4" rx="1"/><path d="M12 8v4H7v4M12 12h5v4"/>',
      gear: '<circle cx="12" cy="12" r="3"/><path d="M12 3v3M12 18v3M3 12h3M18 12h3M5.6 5.6l2.1 2.1M16.3 16.3l2.1 2.1M18.4 5.6l-2.1 2.1M7.7 16.3l-2.1 2.1"/>',
      play: '<path d="M8 5v14l11-7z"/>',
      screen: '<rect x="4" y="5" width="16" height="12" rx="2"/><path d="M8 21h8M12 17v4"/>',
      alert: '<path d="M12 4 3.5 19h17z"/><path d="M12 9v4M12 16h.01"/>'
    };

    return paths[name] || paths.document;
  };

  const renderIcon = (name) => `<span class="vpms-nav-icon" aria-hidden="true"><svg viewBox="0 0 24 24" focusable="false">${iconPath(name)}</svg></span>`;
  const sidebarNav = document.querySelector("[data-vpms-sidebar-nav]");
  const sidebarLinks = Array.from((sidebarNav || document).querySelectorAll("[data-sidebar-link]"));

  sidebarLinks.forEach((link) => {
    const label = normalizeLabel(link.textContent);
    const iconName = navIconMap[label] || "document";
    link.dataset.navIcon = "";
    link.dataset.navIconName = iconName;
    if (!link.querySelector(".vpms-nav-icon")) {
      link.insertAdjacentHTML("afterbegin", renderIcon(iconName));
    }
    link.classList.add("has-vpms-icon");
  });

  document.querySelectorAll(".vpms-command-item").forEach((item) => {
    const icon = item.querySelector(".vpms-command-icon");
    if (!icon) {
      return;
    }

    const label = normalizeLabel(item.textContent);
    icon.innerHTML = `<svg viewBox="0 0 24 24" focusable="false">${iconPath(commandIconMap[label] || "document")}</svg>`;
    icon.classList.add("has-vpms-svg");
  });

  const currentTime = document.getElementById("vpmsCurrentTime");
  const currentDate = document.getElementById("vpmsCurrentDate");
  const updateCurrentTime = () => {
    if (!currentTime || !currentDate) {
      return;
    }

    const now = new Date();
    currentTime.textContent = new Intl.DateTimeFormat([], {
      hour: "numeric",
      minute: "2-digit",
      second: "2-digit"
    }).format(now);
    currentTime.dateTime = now.toISOString();
    currentDate.textContent = new Intl.DateTimeFormat([], {
      weekday: "long",
      month: "short",
      day: "numeric",
      year: "numeric"
    }).format(now);
  };

  updateCurrentTime();
  setInterval(updateCurrentTime, 1000);

  const currentPath = window.location.pathname.toLowerCase().replace(/\/$/, "") || "/";
  const navLinks = sidebarLinks;
  const lastClickedLinkStorageKey = "vpms.sidebar.lastClickedLink";
  const favoritesStorageKey = "vpms.sidebar.favorites";
  const recentLinksStorageKey = "vpms.sidebar.recentLinks";
  const getLinkPath = (link) => new URL(link.href, window.location.origin).pathname.toLowerCase().replace(/\/$/, "") || "/";
  const getLinkGroupId = (link) => link.closest("[data-vpms-sidebar-group]")?.dataset.groupId || "";
  const getLinkEntry = (link) => ({
    label: (link.textContent || "").replace(/\s+/g, " ").trim(),
    normalizedLabel: normalizeLabel(link.textContent),
    path: getLinkPath(link),
    href: link.href,
    groupId: getLinkGroupId(link),
    icon: link.dataset.navIconName || navIconMap[normalizeLabel(link.textContent)] || "document"
  });
  const getLastClickedLink = () => {
    try {
      return JSON.parse(localStorage.getItem(lastClickedLinkStorageKey) || "null");
    } catch {
      return null;
    }
  };
  const loadShortcutEntries = (storageKey) => {
    try {
      const entries = JSON.parse(localStorage.getItem(storageKey) || "[]");
      return Array.isArray(entries) ? entries : [];
    } catch {
      return [];
    }
  };
  const availableEntries = navLinks.map(getLinkEntry);
  const findAvailableEntry = (entry) =>
    availableEntries.find((item) => item.path === entry.path && item.normalizedLabel === entry.normalizedLabel) ||
    availableEntries.find((item) => item.path === entry.path);
  const saveRecentEntry = (entry) => {
    const nextEntries = [
      entry,
      ...loadShortcutEntries(recentLinksStorageKey)
        .filter((item) => item.path !== entry.path || item.normalizedLabel !== entry.normalizedLabel)
    ].slice(0, 6);
    localStorage.setItem(recentLinksStorageKey, JSON.stringify(nextEntries));
  };
  const saveRecentLink = (link) => saveRecentEntry(getLinkEntry(link));
  const renderShortcutList = (container, entries, emptyText) => {
    if (!container) {
      return;
    }

    container.innerHTML = "";
    const safeEntries = entries
      .map(findAvailableEntry)
      .filter(Boolean)
      .slice(0, 5);

    if (!safeEntries.length) {
      const empty = document.createElement("span");
      empty.className = "vpms-sidebar-shortcut-empty";
      empty.textContent = emptyText;
      container.appendChild(empty);
      return;
    }

    safeEntries.forEach((entry) => {
      const link = document.createElement("a");
      link.className = "vpms-sidebar-shortcut";
      link.href = entry.href;
      link.innerHTML = `${renderIcon(entry.icon)}<span>${entry.label}</span>`;
      link.addEventListener("click", () => {
        localStorage.setItem(lastClickedLinkStorageKey, JSON.stringify({
          path: entry.path,
          groupId: entry.groupId,
          label: entry.normalizedLabel,
          at: Date.now()
        }));
        saveRecentEntry(entry);
      });
      container.appendChild(link);
    });
  };
  const renderShortcutPanels = () => {
    const favoritesList = document.querySelector("[data-vpms-favorites-list]");
    const recentList = document.querySelector("[data-vpms-recent-list]");
    const storedFavorites = loadShortcutEntries(favoritesStorageKey);
    const defaultFavoriteLabels = ["dashboard", "room rack", "reservations", "folios", "payments", "report center"];
    const favoriteEntries = storedFavorites.length
      ? storedFavorites
      : defaultFavoriteLabels
        .map((label) => availableEntries.find((entry) => entry.normalizedLabel === label))
        .filter(Boolean);

    renderShortcutList(favoritesList, favoriteEntries, "Daily shortcuts appear here.");
    renderShortcutList(recentList, loadShortcutEntries(recentLinksStorageKey), "Open a module to build history.");
  };

  navLinks.forEach((link) => {
    link.addEventListener("click", () => {
      localStorage.setItem(lastClickedLinkStorageKey, JSON.stringify({
        path: getLinkPath(link),
        groupId: getLinkGroupId(link),
        label: normalizeLabel(link.textContent),
        at: Date.now()
      }));
    });
  });

  renderShortcutPanels();

  let activeLink = null;
  const lastClickedLink = getLastClickedLink();
  const activeCandidates = navLinks.filter((link) => {
    const linkPath = getLinkPath(link);
    return linkPath === currentPath || (linkPath !== "/" && currentPath.startsWith(linkPath));
  });

  if (lastClickedLink?.path === currentPath) {
    activeLink = activeCandidates.find((link) =>
      getLinkGroupId(link) === lastClickedLink.groupId &&
      normalizeLabel(link.textContent) === lastClickedLink.label);
  }

  activeCandidates.forEach((link) => {
    const linkPath = getLinkPath(link);
    if (linkPath === currentPath || (linkPath !== "/" && currentPath.startsWith(linkPath))) {
      if (!activeLink || linkPath.length > getLinkPath(activeLink).length) {
        activeLink = link;
      }
    }
  });

  if (activeLink) {
    activeLink.classList.add("active");
    activeLink.setAttribute("aria-current", "page");
  }

  const sidebarGroups = sidebarNav ? Array.from(sidebarNav.querySelectorAll("[data-vpms-sidebar-group]")) : [];
  const sidebarStorageKey = "vpms.sidebar.expandedGroups";
  const sidebarScrollStorageKey = "vpms.sidebar.scrollTop";
  const compactStorageKey = "vpms.sidebar.compact";

  const setGroupOpen = (group, isOpen) => {
    const toggle = group.querySelector("[data-vpms-sidebar-toggle]");
    group.classList.toggle("is-open", isOpen);
    if (toggle) {
      toggle.setAttribute("aria-expanded", isOpen ? "true" : "false");
    }
  };

  const closeSidebarGroupsExcept = (openGroup) => {
    sidebarGroups.forEach((group) => {
      if (group !== openGroup) {
        setGroupOpen(group, false);
      }
    });
  };

  const getSavedOpenGroups = () => {
    try {
      const saved = JSON.parse(localStorage.getItem(sidebarStorageKey) || "[]");
      return new Set(Array.isArray(saved) ? saved : []);
    } catch {
      return new Set();
    }
  };

  const saveOpenGroups = () => {
    if (!sidebarGroups.length) {
      return;
    }

    const openIds = sidebarGroups
      .filter((group) => group.classList.contains("is-open"))
      .map((group) => group.dataset.groupId)
      .filter(Boolean);
    localStorage.setItem(sidebarStorageKey, JSON.stringify(openIds));
  };

  if (sidebarGroups.length) {
    const savedValue = localStorage.getItem(sidebarStorageKey);
    const savedOpenGroups = getSavedOpenGroups();
    const activeGroup = activeLink?.closest("[data-vpms-sidebar-group]") || null;

    sidebarGroups.forEach((group) => {
      const shouldOpen = activeGroup
        ? group === activeGroup
        : savedValue === null
          ? group.dataset.groupId === "command-center"
          : savedOpenGroups.has(group.dataset.groupId);
      setGroupOpen(group, shouldOpen);
    });

    if (activeLink) {
      if (activeGroup) {
        activeGroup.classList.add("has-active");
        setGroupOpen(activeGroup, true);
      }
    }

    sidebarGroups.forEach((group) => {
      const toggle = group.querySelector("[data-vpms-sidebar-toggle]");
      if (!toggle) {
        return;
      }

      toggle.addEventListener("click", () => {
        const shouldOpen = !group.classList.contains("is-open");
        if (shouldOpen && !sidebarNav?.classList.contains("is-searching")) {
          closeSidebarGroupsExcept(group);
        }

        setGroupOpen(group, shouldOpen);
        saveOpenGroups();
      });
    });
  }

  if (sidebarNav) {
    let isRestoringSidebarScroll = true;
    let scrollSaveHandle = 0;
    const restoreSidebarScroll = () => {
      const savedScrollTop = Number.parseInt(localStorage.getItem(sidebarScrollStorageKey) || "0", 10);
      if (!Number.isNaN(savedScrollTop) && savedScrollTop > 0) {
        sidebarNav.scrollTop = savedScrollTop;
      } else if (activeLink) {
        const activeGroup = activeLink.closest("[data-vpms-sidebar-group]");
        activeGroup?.scrollIntoView({ block: "nearest" });
      }

      isRestoringSidebarScroll = false;
    };

    requestAnimationFrame(() => requestAnimationFrame(restoreSidebarScroll));

    sidebarNav.addEventListener("scroll", () => {
      if (isRestoringSidebarScroll) {
        return;
      }

      window.clearTimeout(scrollSaveHandle);
      scrollSaveHandle = window.setTimeout(() => {
        localStorage.setItem(sidebarScrollStorageKey, String(sidebarNav.scrollTop));
      }, 120);
    }, { passive: true });

    sidebarLinks.forEach((link) => {
      link.addEventListener("click", () => {
        localStorage.setItem(sidebarScrollStorageKey, String(sidebarNav.scrollTop));
      });
    });

    window.addEventListener("beforeunload", () => {
      localStorage.setItem(sidebarScrollStorageKey, String(sidebarNav.scrollTop));
    });
  }

  const searchInput = document.getElementById("sidebarSearch");
  if (searchInput && sidebarNav) {
    searchInput.addEventListener("input", () => {
      const query = searchInput.value.trim().toLowerCase();

      sidebarNav.classList.toggle("is-searching", query.length > 0);
      sidebarGroups.forEach((section) => {
        let visibleLinks = 0;
        const links = Array.from(section.querySelectorAll("[data-sidebar-link]"));
        const groupTitle = normalizeLabel(section.querySelector(".vpms-sidebar-group-title")?.textContent);
        const groupMatches = query.length > 0 && groupTitle.includes(query);

        links.forEach((link) => {
          const isMatch = !query || groupMatches || normalizeLabel(link.textContent).includes(query);
          link.classList.toggle("d-none", !isMatch);
          if (isMatch) {
            visibleLinks += 1;
          }
        });

        section.classList.toggle("is-filtered-empty", query.length > 0 && visibleLinks === 0);
        section.classList.toggle("is-search-open", query.length > 0 && visibleLinks > 0);
      });
    });

    document.addEventListener("keydown", (event) => {
      if (event.key !== "/" || event.ctrlKey || event.metaKey || event.altKey) {
        return;
      }

      const target = event.target;
      if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target?.isContentEditable) {
        return;
      }

      event.preventDefault();
      searchInput.focus();
    });
  }

  const sidebarOffcanvasElement = document.getElementById("appSidebar");
  const closeMobileSidebar = () => {
    if (!sidebarOffcanvasElement || window.innerWidth >= 992 || !window.bootstrap?.Offcanvas) {
      return;
    }

    const instance = window.bootstrap.Offcanvas.getInstance(sidebarOffcanvasElement);
    instance?.hide();
  };

  navLinks.forEach((link) => {
    link.addEventListener("click", closeMobileSidebar);
  });

  const compactToggle = document.getElementById("sidebarCompactToggle");
  const setSidebarCompact = (isCompact) => {
    document.body.classList.toggle("vpms-sidebar-collapsed", isCompact);
    if (compactToggle) {
      compactToggle.setAttribute("aria-pressed", isCompact ? "true" : "false");
      const label = compactToggle.querySelector(".vpms-sidebar-compact-text");
      if (label) {
        label.textContent = isCompact ? "Expand sidebar" : "Collapse sidebar";
      }
    }
  };

  setSidebarCompact(localStorage.getItem(compactStorageKey) === "true");

  if (compactToggle) {
    compactToggle.addEventListener("click", () => {
      const nextState = !document.body.classList.contains("vpms-sidebar-collapsed");
      setSidebarCompact(nextState);
      localStorage.setItem(compactStorageKey, nextState ? "true" : "false");
    });
  }

  const normalizeStatus = (value) =>
    (value || "")
      .trim()
      .replace(/([a-z])([A-Z])/g, "$1-$2")
      .replace(/&/g, "and")
      .replace(/[^a-zA-Z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "")
      .toLowerCase();

  document.querySelectorAll(".badge, .status-badge, .badge-status").forEach((badge) => {
    const status = normalizeStatus(badge.textContent);
    if (!status) {
      return;
    }

    badge.classList.add("vpms-status-badge", `status-${status}`, `status-${status.replace(/-/g, "")}`);
  });

  document.querySelectorAll(".vpms-content table.table").forEach((table) => {
    table.classList.add("vpms-table");
    table.dataset.vpmsEnhanced = "true";

    const parent = table.parentElement;
    if (!parent || parent.classList.contains("table-responsive") || parent.classList.contains("vpms-table-wrapper")) {
      parent?.classList.add("vpms-table-wrapper");
      return;
    }

    const wrapper = document.createElement("div");
    wrapper.className = "vpms-table-wrapper table-responsive";
    table.parentNode.insertBefore(wrapper, table);
    wrapper.appendChild(table);
  });

  document.querySelectorAll(".vpms-content form.row.g-3, .vpms-content form.border.rounded, .vpms-content > form.mt-3").forEach((form) => {
    if (!form.classList.contains("d-inline") && !form.classList.contains("d-flex")) {
      form.classList.add("vpms-form-card");
    }
  });

  document.querySelectorAll(".vpms-content .border.rounded.p-3, .vpms-content .border.rounded.p-2").forEach((card) => {
    card.classList.add("vpms-stat-card");
  });

  document.querySelectorAll(".modal").forEach((modal) => {
    if (modal.parentElement !== document.body) {
      document.body.appendChild(modal);
    }

    modal.addEventListener("show.bs.modal", () => {
      document.body.classList.add("vpms-modal-active");
    });

    modal.addEventListener("hidden.bs.modal", () => {
      if (!document.querySelector(".modal.show")) {
        document.body.classList.remove("vpms-modal-active");
      }
    });
  });

  const workflowDialog = document.getElementById("vpmsWorkflowDialog");
  const workflowDialogFrame = document.getElementById("vpmsWorkflowDialogFrame");
  const workflowDialogTitle = document.getElementById("vpmsWorkflowDialogTitle");
  const workflowDialogOpenFull = document.getElementById("vpmsWorkflowDialogOpenFull");
  const workflowDialogLoading = document.getElementById("vpmsWorkflowDialogLoading");
  const workflowDialogInstance = workflowDialog && window.bootstrap
    ? bootstrap.Modal.getOrCreateInstance(workflowDialog)
    : null;
  const nativeWorkflowDialog = document.getElementById("vpmsNativeWorkflowDialog");
  const nativeWorkflowDialogTitle = document.getElementById("vpmsNativeWorkflowDialogTitle");
  const nativeWorkflowDialogOpenFull = document.getElementById("vpmsNativeWorkflowDialogOpenFull");
  const nativeWorkflowDialogLoading = document.getElementById("vpmsNativeWorkflowDialogLoading");
  const nativeWorkflowDialogContent = document.getElementById("vpmsNativeWorkflowDialogContent");
  const nativeWorkflowDialogInstance = nativeWorkflowDialog && window.bootstrap
    ? bootstrap.Modal.getOrCreateInstance(nativeWorkflowDialog)
    : null;
  let workflowDialogInitialPath = "";
  let workflowDialogHasLoaded = false;
  let nativeWorkflowDialogUrl = "";
  const isInWorkflowDialogFrame = document.body.classList.contains("vpms-dialog-frame");

  const isInternalWorkflowUrl = (url) =>
    url.origin === window.location.origin &&
    !url.pathname.startsWith("/Identity/") &&
    !url.pathname.startsWith("/Booking/") &&
    !url.pathname.startsWith("/GuestPortal/") &&
    !url.pathname.includes("/Print");

  if (isInWorkflowDialogFrame) {
    document.addEventListener("submit", (event) => {
      const form = event.target;
      if (!(form instanceof HTMLFormElement)) {
        return;
      }

      try {
        const actionUrl = new URL(form.action || window.location.href, window.location.origin);
        if (!isInternalWorkflowUrl(actionUrl)) {
          return;
        }

        actionUrl.searchParams.set("vpmsDialog", "1");
        form.action = actionUrl.toString();
      } catch {
        // Preserve the standard submit if the browser cannot parse the action.
      }
    }, true);

    document.addEventListener("click", (event) => {
      const link = event.target?.closest?.("a[href]");
      if (!link || event.defaultPrevented || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) {
        return;
      }

      const label = normalizeLabel(link.textContent);
      if (/^(back|cancel|return|close)$/.test(label) || link.dataset.vpmsDialogClose !== undefined) {
        event.preventDefault();
        window.parent?.postMessage({ type: "vpms:workflow-dialog-close", reload: false }, window.location.origin);
        return;
      }

      if (link.target || link.hasAttribute("download") || link.dataset.vpmsNoDialog !== undefined) {
        return;
      }

      try {
        const url = new URL(link.href, window.location.origin);
        if (!isInternalWorkflowUrl(url)) {
          return;
        }

        url.searchParams.set("vpmsDialog", "1");
        event.preventDefault();
        window.location.href = url.toString();
      } catch {
        // Keep default navigation when a URL cannot be normalized.
      }
    });
  }

  const setNativeWorkflowLoading = (isLoading) => {
    nativeWorkflowDialog?.classList.toggle("is-submitting", isLoading);
    nativeWorkflowDialog?.classList.toggle("is-loaded", !isLoading);
    nativeWorkflowDialogLoading?.classList.toggle("d-none", !isLoading);
  };

  const buildDialogUrl = (href, options = {}) => {
    const url = new URL(href, window.location.origin);
    url.searchParams.set("vpmsDialog", "1");
    url.searchParams.set("vpmsNative", "1");
    if (options.handler) {
      url.searchParams.set("handler", options.handler);
    }
    return url;
  };

  const extractNativeWorkflowContent = async (response, fallbackUrl) => {
    const html = await response.text();
    const documentFragment = new DOMParser().parseFromString(html, "text/html");
    const content = documentFragment.querySelector(".vpms-dialog-frame-content") ||
      documentFragment.querySelector(".vpms-content") ||
      documentFragment.body;

    if (!content) {
      return `<div class="vpms-native-workflow-error"><strong>Workflow unavailable.</strong><span>The response did not contain a usable form.</span></div>`;
    }

    const wrapper = document.createElement("div");
    wrapper.innerHTML = content.innerHTML;

    wrapper.querySelectorAll("form").forEach((form) => {
      const action = form.getAttribute("action");
      const normalizedAction = action
        ? new URL(action, fallbackUrl).toString()
        : fallbackUrl.toString();
      form.action = normalizedAction;
      form.dataset.vpmsNativeWorkflowForm = "";
    });

    wrapper.querySelectorAll("a[href]").forEach((link) => {
      const href = link.getAttribute("href");
      if (!href || href.startsWith("#") || href.startsWith("mailto:") || href.startsWith("tel:")) {
        return;
      }

      try {
        link.href = new URL(href, fallbackUrl).toString();
      } catch {
        // Leave malformed links untouched.
      }
    });

    return wrapper.innerHTML;
  };

  const prepareNativeWorkflowContent = () => {
    if (!nativeWorkflowDialogContent) {
      return;
    }

    const firstError = nativeWorkflowDialogContent.querySelector(
      ".validation-summary-errors, .field-validation-error:not(:empty), .text-danger:not(:empty), .alert-danger:not(.validation-summary-valid):not(:empty)"
    );
    const firstInvalidField = nativeWorkflowDialogContent.querySelector(
      ".input-validation-error, [aria-invalid='true'], .is-invalid"
    );

    if (firstError) {
      firstError.classList.add("vpms-native-validation-focus");
      firstError.scrollIntoView({ behavior: "smooth", block: "center" });
      if (firstInvalidField instanceof HTMLElement) {
        window.setTimeout(() => firstInvalidField.focus({ preventScroll: true }), 150);
      }
      return;
    }

    const firstField = nativeWorkflowDialogContent.querySelector(
      "select:not([disabled]), input:not([type='hidden']):not([disabled]), textarea:not([disabled])"
    );
    if (firstField instanceof HTMLElement) {
      window.setTimeout(() => firstField.focus({ preventScroll: true }), 150);
    }
  };

  const loadNativeWorkflowDialog = async (href, title, trigger) => {
    if (!nativeWorkflowDialogInstance || !nativeWorkflowDialogContent) {
      return false;
    }

    const url = buildDialogUrl(href, { handler: trigger?.dataset?.vpmsNativeHandler });
    nativeWorkflowDialogUrl = url.toString();

    if (nativeWorkflowDialogTitle) {
      nativeWorkflowDialogTitle.textContent = title || "Quick Workflow";
    }

    if (nativeWorkflowDialogOpenFull) {
      nativeWorkflowDialogOpenFull.href = href;
    }

    nativeWorkflowDialogContent.innerHTML = "";
    setNativeWorkflowLoading(true);
    nativeWorkflowDialogInstance.show();

    try {
      const response = await fetch(url.toString(), {
        credentials: "same-origin",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          "X-VPMS-Native-Dialog": "1"
        }
      });

      if (!response.ok) {
        throw new Error(`Workflow returned ${response.status}.`);
      }

      nativeWorkflowDialogContent.innerHTML = await extractNativeWorkflowContent(response, url);
      prepareNativeWorkflowContent();
    } catch (error) {
      nativeWorkflowDialogContent.innerHTML = `<div class="vpms-native-workflow-error"><strong>Workflow could not be opened.</strong><span>${error.message}</span></div>`;
    } finally {
      setNativeWorkflowLoading(false);
    }

    return true;
  };

  const submitNativeWorkflowForm = async (form) => {
    if (!nativeWorkflowDialogContent) {
      return;
    }

    let actionUrl;
    try {
      actionUrl = buildDialogUrl(form.action || nativeWorkflowDialogUrl || window.location.href);
    } catch {
      nativeWorkflowDialogContent.insertAdjacentHTML("afterbegin", `<div class="vpms-native-workflow-error"><strong>Could not submit workflow.</strong><span>The form action could not be resolved.</span></div>`);
      return;
    }

    setNativeWorkflowLoading(true);

    try {
      const response = await fetch(actionUrl.toString(), {
        method: (form.method || "GET").toUpperCase(),
        body: (form.method || "GET").toUpperCase() === "GET" ? null : new FormData(form),
        credentials: "same-origin",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          "X-VPMS-Native-Dialog": "1"
        }
      });

      if (response.redirected) {
        nativeWorkflowDialogInstance?.hide();
        window.location.reload();
        return;
      }

      if (!response.ok) {
        throw new Error(`Workflow returned ${response.status}.`);
      }

      nativeWorkflowDialogContent.innerHTML = await extractNativeWorkflowContent(response, actionUrl);
      prepareNativeWorkflowContent();
    } catch (error) {
      nativeWorkflowDialogContent.insertAdjacentHTML("afterbegin", `<div class="vpms-native-workflow-error"><strong>Workflow could not be submitted.</strong><span>${error.message}</span></div>`);
    } finally {
      setNativeWorkflowLoading(false);
    }
  };

  const isNativeWorkflowDialogCandidate = (link) => {
    if (!nativeWorkflowDialogInstance || !nativeWorkflowDialogContent || !link?.href) {
      return false;
    }

    if (link.dataset.vpmsNativeDialog === undefined ||
      link.dataset.vpmsNoDialog !== undefined ||
      link.dataset.quickReservation === "true" ||
      link.target ||
      link.hasAttribute("download") ||
      link.closest(".modal")) {
      return false;
    }

    try {
      return isInternalWorkflowUrl(new URL(link.href, window.location.origin));
    } catch {
      return false;
    }
  };

  const isWorkflowDialogCandidate = (link) => {
    if (!workflowDialogInstance || !workflowDialogFrame || !link?.href) {
      return false;
    }

    const parentModal = link.closest(".modal");
    if (link.dataset.vpmsNoDialog !== undefined ||
      link.dataset.quickReservation === "true" ||
      link.target ||
      link.hasAttribute("download") ||
      (parentModal && parentModal.id !== "adminSetupGuide")) {
      return false;
    }

    let url;
    try {
      url = new URL(link.href, window.location.origin);
    } catch {
      return false;
    }

    if (url.origin !== window.location.origin ||
      url.pathname.startsWith("/Identity/") ||
      url.pathname.startsWith("/Booking/") ||
      url.pathname.startsWith("/GuestPortal/") ||
      url.pathname.includes("/Print")) {
      return false;
    }

    if (link.dataset.vpmsDialog !== undefined) {
      return true;
    }

    if (link.closest(".vpms-room-calendar-shell")) {
      return false;
    }

    const isInOperationalSurface = link.closest(".vpms-content, .app-commandbar") !== null;
    const label = normalizeLabel(link.textContent);
    const path = url.pathname.toLowerCase();
    const createLikePath = /\/(create|postcharge|postpayment|checkin|checkout|close)$/.test(path);
    const createLikeLabel = /^(new|create|add|post|check in|check out|allocate)/.test(label);

    return isInOperationalSurface && createLikePath && createLikeLabel;
  };

  const openWorkflowDialog = (link) => {
    const url = new URL(link.href, window.location.origin);
    workflowDialogInitialPath = url.pathname.toLowerCase();
    workflowDialogHasLoaded = false;
    url.searchParams.set("vpmsDialog", "1");

    if (workflowDialogTitle) {
      workflowDialogTitle.textContent = link.dataset.vpmsDialogTitle || (link.textContent || "Quick Workflow").trim() || "Quick Workflow";
    }

    if (workflowDialogOpenFull) {
      workflowDialogOpenFull.href = link.href;
    }

    const parentModal = link.closest(".modal");
    if (parentModal?.id === "adminSetupGuide" && window.bootstrap) {
      window.bootstrap.Modal.getInstance(parentModal)?.hide();
    }

    workflowDialog?.classList.remove("is-loaded");
    workflowDialogLoading?.classList.remove("d-none");
    workflowDialogFrame.src = url.toString();
    workflowDialogInstance.show();
  };

  document.addEventListener("click", (event) => {
    const link = event.target?.closest?.("a[href]");
    if (!link || event.defaultPrevented || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) {
      return;
    }

    if (!isNativeWorkflowDialogCandidate(link)) {
      return;
    }

    event.preventDefault();
    loadNativeWorkflowDialog(link.href, link.dataset.vpmsNativeDialogTitle || (link.textContent || "Quick Workflow").trim(), link);
  });

  nativeWorkflowDialogContent?.addEventListener("submit", (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) {
      return;
    }

    event.preventDefault();
    submitNativeWorkflowForm(form);
  });

  nativeWorkflowDialogContent?.addEventListener("click", (event) => {
    const link = event.target?.closest?.("a[href]");
    if (!link || event.defaultPrevented || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) {
      return;
    }

    const label = normalizeLabel(link.textContent);
    if (/^(back|cancel|return|close)$/.test(label) || link.dataset.vpmsDialogClose !== undefined) {
      event.preventDefault();
      nativeWorkflowDialogInstance?.hide();
      return;
    }

    if (link.target || link.hasAttribute("download") || link.dataset.vpmsNoDialog !== undefined) {
      return;
    }

    let url;
    try {
      url = new URL(link.href, window.location.origin);
    } catch {
      return;
    }

    if (!isInternalWorkflowUrl(url)) {
      return;
    }

    event.preventDefault();
    loadNativeWorkflowDialog(url.toString(), link.dataset.vpmsNativeDialogTitle || (link.textContent || "Quick Workflow").trim(), link);
  });

  nativeWorkflowDialog?.addEventListener("hidden.bs.modal", () => {
    if (nativeWorkflowDialogContent) {
      nativeWorkflowDialogContent.innerHTML = "";
    }

    nativeWorkflowDialogUrl = "";
    nativeWorkflowDialog?.classList.remove("is-loaded", "is-submitting");
    nativeWorkflowDialogLoading?.classList.remove("d-none");
  });

  document.addEventListener("click", (event) => {
    const link = event.target?.closest?.("a[href]");
    if (!link || event.defaultPrevented || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) {
      return;
    }

    if (!isWorkflowDialogCandidate(link)) {
      return;
    }

    event.preventDefault();
    openWorkflowDialog(link);
  });

  workflowDialogFrame?.addEventListener("load", () => {
    workflowDialog?.classList.add("is-loaded");
    workflowDialogLoading?.classList.add("d-none");

    let frameUrl;
    try {
      frameUrl = new URL(workflowDialogFrame.contentWindow.location.href);
    } catch {
      return;
    }

    const isBlank = frameUrl.href === "about:blank";
    if (isBlank) {
      return;
    }

    const isDialogFrame = frameUrl.searchParams.get("vpmsDialog") === "1";
    const framePath = frameUrl.pathname.toLowerCase();
    if (workflowDialogHasLoaded && !isDialogFrame && framePath !== workflowDialogInitialPath) {
      workflowDialogInstance?.hide();
      window.location.reload();
      return;
    }

    workflowDialogHasLoaded = true;
  });

  window.addEventListener("message", (event) => {
    if (event.origin !== window.location.origin || event.data?.type !== "vpms:workflow-dialog-close") {
      return;
    }

    workflowDialogInstance?.hide();
    if (event.data.reload) {
      window.location.reload();
    }
  });

  workflowDialog?.addEventListener("hidden.bs.modal", () => {
    if (workflowDialogFrame) {
      workflowDialogFrame.src = "about:blank";
    }

    workflowDialog?.classList.remove("is-loaded");
    workflowDialogLoading?.classList.remove("d-none");
    workflowDialogInitialPath = "";
    workflowDialogHasLoaded = false;
  });

  const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const interactiveSurfaceSelector = [
    ".vpms-card",
    ".vpms-operational-panel",
    ".vpms-refined-card",
    ".vpms-form-section",
    ".vpms-form-card",
    ".vpms-table-wrapper",
    ".vpms-room-card-premium",
    ".vpms-calendar-filter-card",
    ".vpms-calendar-legend-panel",
    ".vpms-signal-strip > article",
    ".vpms-control-strip > article",
    ".vpms-calendar-command-strip > article",
    ".vpms-command-brief > article",
    ".vpms-board-brief > article",
    ".vpms-property-card",
    ".vpms-booking-card",
    ".vpms-modal-signal-grid > article",
    ".vpms-modal-action-grid > a"
  ].join(", ");

  document.querySelectorAll(interactiveSurfaceSelector).forEach((surface) => {
    surface.classList.add("vpms-interactive-surface");

    if (prefersReducedMotion) {
      return;
    }

    surface.addEventListener("pointermove", (event) => {
      const rect = surface.getBoundingClientRect();
      const x = ((event.clientX - rect.left) / rect.width) * 100;
      const y = ((event.clientY - rect.top) / rect.height) * 100;
      surface.style.setProperty("--vpms-pointer-x", `${x.toFixed(2)}%`);
      surface.style.setProperty("--vpms-pointer-y", `${y.toFixed(2)}%`);
    });
  });

  if (!prefersReducedMotion) {
    const revealTargets = Array.from(document.querySelectorAll([
      ".vpms-content > section",
      ".vpms-content > div",
      ".vpms-content > form",
      ".vpms-content > main",
      ".vpms-refined-main > section",
      ".vpms-refined-side > section",
      ".vpms-stack > section"
    ].join(", "))).filter((element) => !element.classList.contains("modal"));

    const revealObserver = "IntersectionObserver" in window
      ? new IntersectionObserver((entries, observer) => {
          entries.forEach((entry) => {
            if (!entry.isIntersecting) {
              return;
            }

            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
          });
        }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 })
      : null;

    revealTargets.forEach((element, index) => {
      element.classList.add("vpms-reveal");
      element.style.setProperty("--vpms-reveal-delay", `${Math.min(index * 32, 220)}ms`);

      if (revealObserver) {
        revealObserver.observe(element);
      } else {
        element.classList.add("is-visible");
      }
    });

    document.querySelectorAll(".btn:not(.dropdown-toggle)").forEach((button) => {
      button.addEventListener("pointerdown", (event) => {
        if (button.disabled || button.classList.contains("disabled")) {
          return;
        }

        const rect = button.getBoundingClientRect();
        const ripple = document.createElement("span");
        ripple.className = "vpms-ripple";
        ripple.style.left = `${event.clientX - rect.left}px`;
        ripple.style.top = `${event.clientY - rect.top}px`;
        button.appendChild(ripple);
        ripple.addEventListener("animationend", () => ripple.remove(), { once: true });
      });
    });
  }

  document.body.classList.add("vpms-ui-ready");
});
