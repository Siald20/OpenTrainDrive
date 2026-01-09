(() => {
  const canvas = document.getElementById('otd-plan-canvas');
  if (!canvas) return;

  const editor = document.getElementById('otd-plan-editor');
  const statusEl = document.getElementById('otd-plan-status');
  const palette = document.getElementById('otd-plan-palette');
  const searchInput = document.getElementById('otd-plan-search');
  const configBody = document.getElementById('otd-plan-config-body');
  const addFieldBtn = document.getElementById('otd-plan-add-field');
  const saveBtn = document.getElementById('otd-plan-save');
  const deleteBtn = document.getElementById('otd-plan-delete');
  const duplicateBtn = document.getElementById('otd-plan-duplicate');
  const closeBtn = document.getElementById('otd-plan-close');
  const contextMenu = document.getElementById('otd-plan-context');
  const contextEdit = document.getElementById('otd-plan-context-edit');
  const contextDelete = document.getElementById('otd-plan-context-delete');
  const contextDuplicate = document.getElementById('otd-plan-context-duplicate');
  const objectWindow = document.getElementById('otd-plan-object');
  const objectBody = document.getElementById('otd-plan-object-body');
  const objectClose = document.getElementById('otd-plan-object-close');
  const zoomOutBtn = document.getElementById('otd-plan-zoom-out');
  const zoomInBtn = document.getElementById('otd-plan-zoom-in');
  const zoomResetBtn = document.getElementById('otd-plan-zoom-reset');
  const zoomLevel = document.getElementById('otd-plan-zoom-level');

  const menuEdit = document.getElementById('otd-menu-edit');
  const menuSave = document.getElementById('otd-menu-save');

  const state = {
    gridSize: 48,
    symbols: [],
    selectedId: null,
    editMode: false,
    scale: 1,
    drag: null,
    paletteItems: [],
    paletteFilter: ''
  };

  const configFields = [
    { key: 'name', label: 'Name', type: 'text' },
    { key: 'shortName', label: 'Kurzname', type: 'text' },
    { key: 'type', label: 'Typ', type: 'text' },
    { key: 'classes', label: 'CSS Klassen', type: 'text' },
    { key: 'layer', label: 'Ebene', type: 'number' },
    { key: 'rotation', label: 'Rotation', type: 'number' },
    { key: 'direction', label: 'Richtung', type: 'text' },
    { key: 'address', label: 'addresse', type: 'text' },
    { key: 'protocol', label: 'Protokoll', type: 'text' },
    { key: 'length', label: 'Laenge', type: 'number' },
    { key: 'vmax', label: 'Vmax', type: 'number' },
    { key: 'vmin', label: 'Vmin', type: 'number' },
    { key: 'block', label: 'Block', type: 'text' },
    { key: 'section', label: 'Abschnitt', type: 'text' },
    { key: 'route', label: 'Fahrstrasse', type: 'text' },
    { key: 'signal', label: 'Signal', type: 'text' },
    { key: 'turnout', label: 'Weiche', type: 'text' },
    { key: 'feedback', label: 'Rueckmelder', type: 'text' },
    { key: 'sensor', label: 'Sensor', type: 'text' },
    { key: 'speed', label: 'Geschwindigkeit', type: 'number' },
    { key: 'state', label: 'Zustand', type: 'text' },
    { key: 'color', label: 'Farbe', type: 'text' },
    { key: 'group', label: 'Gruppe', type: 'text' },
    { key: 'notes', label: 'Notizen', type: 'textarea' }
  ];

  function setStatus(text) {
    if (statusEl) statusEl.textContent = text;
  }

  function setEditMode(enabled) {
    if (!canEditPlan()) {
      setStatus('Nur Admin darf den Gleisplan bearbeiten');
      return;
    }
    state.editMode = enabled;
    canvas.classList.toggle('is-editing', enabled);
    if (editor) {
      editor.classList.toggle('otd-hidden', !enabled);
    }
    if (palette) {
      palette.parentElement?.classList.toggle('is-disabled', !enabled);
    }
    renderPalette();
  }

  function getSelectedSymbol() {
    return state.symbols.find(s => s.id === state.selectedId) || null;
  }

  function applyGridVars() {
    canvas.style.setProperty('--otd-plan-grid', `${state.gridSize}px`);
    canvas.style.setProperty('--otd-symbol-size', `${state.gridSize}px`);
    canvas.style.transform = `scale(${state.scale})`;
    canvas.style.transformOrigin = 'top left';
    if (zoomLevel) {
      zoomLevel.textContent = `${Math.round(state.scale * 100)}%`;
    }
  }

  function render() {
    applyGridVars();
    canvas.innerHTML = '';
    for (const sym of state.symbols) {
      const el = document.createElement('div');
      el.className = `otd-plan-symbol otd-symbol ${sym.classes || ''}`.trim();
      el.dataset.id = sym.id;
      el.style.left = `${sym.x * state.gridSize}px`;
      el.style.top = `${sym.y * state.gridSize}px`;
      if (sym.id === state.selectedId) {
        el.classList.add('is-selected');
      }
      canvas.appendChild(el);
    }
  }

  function renderConfig() {
    if (!configBody) return;
    configBody.innerHTML = '';
    const sym = getSelectedSymbol();
    if (!sym) {
      const empty = document.createElement('div');
      empty.textContent = 'Kein Symbol ausgewaehlt.';
      configBody.appendChild(empty);
      return;
    }

    if (!sym.config) {
      sym.config = { extra: [] };
    }
    if (!Array.isArray(sym.config.extra)) {
      sym.config.extra = [];
    }

    const idRow = document.createElement('div');
    idRow.className = 'otd-plan-config-row';
    idRow.innerHTML = `<label>ID</label><input type="text" value="${sym.id}" readonly />`;
    configBody.appendChild(idRow);

    for (const field of configFields) {
      const row = document.createElement('div');
      row.className = 'otd-plan-config-row';
      const value = sym.config[field.key] ?? (field.key === 'classes' ? sym.classes : '');
      let inputEl;
      if (field.type === 'textarea') {
        inputEl = document.createElement('textarea');
        inputEl.value = value;
      } else {
        inputEl = document.createElement('input');
        inputEl.type = field.type;
        inputEl.value = value;
      }
      inputEl.addEventListener('input', () => {
        if (field.key === 'classes') {
          sym.classes = inputEl.value.trim();
          sym.config[field.key] = inputEl.value.trim();
          render();
        } else {
          sym.config[field.key] = inputEl.value;
        }
      });
      row.appendChild(document.createElement('label')).textContent = field.label;
      row.appendChild(inputEl);
      configBody.appendChild(row);
    }

    for (const extra of sym.config.extra) {
      const row = document.createElement('div');
      row.className = 'otd-plan-config-row';
      const wrapper = document.createElement('div');
      wrapper.className = 'otd-plan-extra-row';
      const keyInput = document.createElement('input');
      keyInput.placeholder = 'Schluessel';
      keyInput.value = extra.key || '';
      const valueInput = document.createElement('input');
      valueInput.placeholder = 'Wert';
      valueInput.value = extra.value || '';
      const removeBtn = document.createElement('button');
      removeBtn.type = 'button';
      removeBtn.textContent = 'X';

      keyInput.addEventListener('input', () => { extra.key = keyInput.value; });
      valueInput.addEventListener('input', () => { extra.value = valueInput.value; });
      removeBtn.addEventListener('click', () => {
        sym.config.extra = sym.config.extra.filter(e => e !== extra);
        renderConfig();
      });

      wrapper.appendChild(keyInput);
      wrapper.appendChild(valueInput);
      wrapper.appendChild(removeBtn);
      row.appendChild(document.createElement('label')).textContent = 'Extra';
      row.appendChild(wrapper);
      configBody.appendChild(row);
    }
  }

  function renderObjectWindow() {
    if (!objectBody || !objectWindow) return;
    objectBody.innerHTML = '';
    const sym = getSelectedSymbol();
    if (!sym) {
      objectWindow.classList.add('otd-hidden');
      return;
    }
    objectWindow.classList.remove('otd-hidden');

    const typeHint = (sym.type || sym.config?.type || sym.classes || '').toLowerCase();
    const typeFields = [];
    if (typeHint.includes('signal')) {
      typeFields.push(
        { key: 'aspect', label: 'Aspekt', type: 'text' },
        { key: 'signalSpeed', label: 'Signalgeschw.', type: 'number' },
        { key: 'signalGroup', label: 'Signalgruppe', type: 'text' },
        { key: 'locked', label: 'Gesperrt', type: 'text' }
      );
    }
    if (typeHint.includes('weiche') || typeHint.includes('turnout')) {
      typeFields.push(
        { key: 'position', label: 'Stellung', type: 'text' },
        { key: 'motor', label: 'Antrieb', type: 'text' },
        { key: 'feedbackOk', label: 'Rueckmeldung', type: 'text' }
      );
    }
    if (typeHint.includes('gleis') || typeHint.includes('track')) {
      typeFields.push(
        { key: 'trackType', label: 'Gleisklasse', type: 'text' },
        { key: 'km', label: 'Kilometer', type: 'text' },
        { key: 'occupied', label: 'Belegung', type: 'text' }
      );
    }

    const fields = [...configFields, ...typeFields];
    for (const field of fields) {
      const row = document.createElement('div');
      row.className = 'otd-plan-config-row';
      const value = sym.config?.[field.key] ?? (field.key === 'classes' ? sym.classes : '');
      let inputEl;
      if (field.type === 'textarea') {
        inputEl = document.createElement('textarea');
        inputEl.value = value;
      } else {
        inputEl = document.createElement('input');
        inputEl.type = field.type;
        inputEl.value = value;
      }
      inputEl.addEventListener('input', () => {
        if (!sym.config) sym.config = { extra: [] };
        sym.config[field.key] = inputEl.value;
        if (field.key === 'classes') {
          sym.classes = inputEl.value.trim();
          render();
        }
      });
      row.appendChild(document.createElement('label')).textContent = field.label;
      row.appendChild(inputEl);
      objectBody.appendChild(row);
    }
  }

  function selectSymbol(id) {
    state.selectedId = id;
    render();
    renderConfig();
    if (objectWindow && !objectWindow.classList.contains('otd-hidden')) {
      renderObjectWindow();
    }
  }

  function addSymbol(payload, position) {
    const sym = {
      id: crypto.randomUUID(),
      type: payload.type || '',
      classes: payload.classes || '',
      x: position.x,
      y: position.y,
      config: {
        name: payload.name || '',
        type: payload.type || '',
        classes: payload.classes || '',
        extra: []
      }
    };
    state.symbols.push(sym);
    selectSymbol(sym.id);
    setStatus('Symbol hinzugefuegt');
  }

  function duplicateSelected() {
    const sym = getSelectedSymbol();
    if (!sym) return;
    const clone = {
      ...sym,
      id: crypto.randomUUID(),
      x: sym.x + 1,
      y: sym.y + 1,
      config: {
        ...sym.config,
        extra: Array.isArray(sym.config?.extra) ? sym.config.extra.map(e => ({ ...e })) : []
      }
    };
    state.symbols.push(clone);
    selectSymbol(clone.id);
    setStatus('Dupliziert');
  }

  function deleteSelected() {
    if (!state.selectedId) return;
    state.symbols = state.symbols.filter(s => s.id !== state.selectedId);
    state.selectedId = null;
    render();
    renderConfig();
    setStatus('Geloescht');
  }

  function showContextMenu(x, y) {
    if (!contextMenu) return;
    contextMenu.style.left = `${x}px`;
    contextMenu.style.top = `${y}px`;
    contextMenu.classList.remove('otd-hidden');
  }

  function hideContextMenu() {
    contextMenu?.classList.add('otd-hidden');
  }

  function getCanvasPosition(evt) {
    const rect = canvas.getBoundingClientRect();
    const x = (evt.clientX - rect.left) / state.scale / state.gridSize;
    const y = (evt.clientY - rect.top) / state.scale / state.gridSize;
    return {
      x: Math.max(0, Math.round(x)),
      y: Math.max(0, Math.round(y))
    };
  }

  function handlePointerDown(evt) {
    if (!state.editMode || evt.button !== 0) return;
    const target = evt.target.closest('.otd-plan-symbol');
    if (!target) return;
    const sym = state.symbols.find(s => s.id === target.dataset.id);
    if (!sym) return;
    selectSymbol(sym.id);
    const start = getCanvasPosition(evt);
    state.drag = {
      id: sym.id,
      originX: sym.x,
      originY: sym.y,
      startX: start.x,
      startY: start.y
    };
    canvas.setPointerCapture(evt.pointerId);
  }

  function handlePointerMove(evt) {
    if (!state.drag) return;
    const pos = getCanvasPosition(evt);
    const dx = pos.x - state.drag.startX;
    const dy = pos.y - state.drag.startY;
    const sym = state.symbols.find(s => s.id === state.drag.id);
    if (!sym) return;
    sym.x = Math.max(0, state.drag.originX + dx);
    sym.y = Math.max(0, state.drag.originY + dy);
    const el = canvas.querySelector(`[data-id="${sym.id}"]`);
    if (el) {
      el.style.left = `${sym.x * state.gridSize}px`;
      el.style.top = `${sym.y * state.gridSize}px`;
    }
  }

  function handlePointerUp(evt) {
    if (!state.drag) return;
    state.drag = null;
    canvas.releasePointerCapture(evt.pointerId);
    setStatus('Position aktualisiert');
  }

  function handleDrop(evt) {
    if (!state.editMode) return;
    evt.preventDefault();
    const raw = evt.dataTransfer?.getData('application/json');
    if (!raw) return;
    let payload;
    try {
      payload = JSON.parse(raw);
    } catch {
      return;
    }
    const pos = getCanvasPosition(evt);
    addSymbol(payload, pos);
  }

  function serializeConfig(sym) {
    const fields = [];
    if (sym.config) {
      for (const [key, value] of Object.entries(sym.config)) {
        if (key === 'extra') continue;
        if (key === 'file') continue;
        if (value !== undefined && value !== null && value !== '') {
          fields.push({ key, value: String(value) });
        }
      }
      if (Array.isArray(sym.config.extra)) {
        for (const extra of sym.config.extra) {
          if (extra.key && extra.key !== 'file') {
            fields.push({ key: String(extra.key), value: String(extra.value ?? '') });
          }
        }
      }
    }
    return fields;
  }

  async function savePlan() {
    try {
      setStatus('Speichere ...');
      const payload = {
        gridSize: state.gridSize,
        symbols: state.symbols.map(sym => ({
          id: sym.id,
          type: sym.type,
          classes: sym.classes,
          x: sym.x,
          y: sym.y,
          config: serializeConfig(sym)
        }))
      };
      const resp = await fetch('/plan/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
      setStatus('Gespeichert');
    } catch (err) {
      setStatus('Speichern fehlgeschlagen');
    }
  }

  function parseConfigFields(node) {
    const config = { extra: [] };
    const fields = node.querySelectorAll('config > field');
      fields.forEach(field => {
        const key = field.getAttribute('key') || '';
        const value = field.getAttribute('value') || '';
        if (!key) return;
        if (key === 'file') return;
        const known = configFields.some(f => f.key === key);
        if (known) {
          config[key] = value;
        } else {
          config.extra.push({ key, value });
        }
      });
    return config;
  }

  async function loadPlan() {
    try {
      const resp = await fetch('/plan.xml', { cache: 'no-store' });
      if (!resp.ok) {
        state.symbols = [];
        render();
        renderConfig();
        setStatus('plan.xml nicht gefunden');
        return;
      }
      const text = await resp.text();
      const doc = new DOMParser().parseFromString(text, 'application/xml');
      const plan = doc.querySelector('plan');
      if (!plan) throw new Error('Kein <plan> Element');
      const gridAttr = plan.getAttribute('grid');
      if (gridAttr) {
        state.gridSize = parseInt(gridAttr, 10) || state.gridSize;
      }
      const symbols = [];
      plan.querySelectorAll('symbols > symbol').forEach(node => {
        const config = parseConfigFields(node);
        const classes = node.getAttribute('classes') || config.classes || '';
        symbols.push({
          id: node.getAttribute('id') || crypto.randomUUID(),
          type: node.getAttribute('type') || config.type || '',
          classes,
          x: parseInt(node.getAttribute('x') || '0', 10),
          y: parseInt(node.getAttribute('y') || '0', 10),
          config
        });
      });
      state.symbols = symbols;
      state.selectedId = symbols.length ? symbols[0].id : null;
      render();
      renderConfig();
      setStatus(`Geladen (${symbols.length} Symbole)`);
    } catch (err) {
      setStatus('Laden fehlgeschlagen');
    }
  }

  async function loadSymbolPalette() {
    try {
      const resp = await fetch('/elements/symbols.css', { cache: 'no-store' });
      if (!resp.ok) throw new Error('symbols.css');
      const text = await resp.text();
      const matches = text.match(/\.otd-symbol--[A-Za-z0-9_-]+/g) || [];
      const unique = Array.from(new Set(matches.map(m => m.replace('.', ''))));
      state.paletteItems = unique.map((className) => ({
        className,
        name: className.replace('otd-symbol--', '')
      }));
      renderPalette();
    } catch (err) {
      setStatus('Symbole nicht geladen');
    }
  }

  function renderPalette() {
    if (!palette) return;
    palette.innerHTML = '';
    const filter = state.paletteFilter.toLowerCase();
    const list = state.paletteItems.filter(item => item.name.toLowerCase().includes(filter));
    for (const itemData of list) {
      const item = document.createElement('div');
      item.className = 'otd-plan-palette-item';
      item.dataset.class = itemData.className || '';
      item.draggable = state.editMode;
      const preview = document.createElement('span');
      preview.className = `otd-symbol ${itemData.className}`;
      item.appendChild(preview);
      item.addEventListener('dragstart', (evt) => {
        if (!state.editMode) return;
        const payload = {
          classes: itemData.className || '',
          type: itemData.name
        };
        evt.dataTransfer?.setData('application/json', JSON.stringify(payload));
        evt.dataTransfer?.setDragImage(item, 24, 24);
      });
      palette.appendChild(item);
    }
  }

  searchInput?.addEventListener('input', () => {
    state.paletteFilter = searchInput.value || '';
    renderPalette();
  });

  addFieldBtn?.addEventListener('click', () => {
    const sym = getSelectedSymbol();
    if (!sym) return;
    if (!sym.config) sym.config = { extra: [] };
    if (!Array.isArray(sym.config.extra)) sym.config.extra = [];
    sym.config.extra.push({ key: '', value: '' });
    renderConfig();
  });

  saveBtn?.addEventListener('click', savePlan);
  menuSave?.addEventListener('click', savePlan);
  deleteBtn?.addEventListener('click', deleteSelected);
  duplicateBtn?.addEventListener('click', duplicateSelected);

  closeBtn?.addEventListener('click', () => setEditMode(false));
  menuEdit?.addEventListener('click', () => setEditMode(true));
  objectClose?.addEventListener('click', () => objectWindow?.classList.add('otd-hidden'));

  contextEdit?.addEventListener('click', () => {
    if (!state.selectedId) return;
    setEditMode(true);
    renderConfig();
    renderObjectWindow();
    hideContextMenu();
  });
  contextDelete?.addEventListener('click', () => {
    deleteSelected();
    hideContextMenu();
  });
  contextDuplicate?.addEventListener('click', () => {
    duplicateSelected();
    hideContextMenu();
  });

  canvas.addEventListener('click', (evt) => {
    const target = evt.target.closest('.otd-plan-symbol');
    if (!target) return;
    selectSymbol(target.dataset.id);
  });

  canvas.addEventListener('contextmenu', (evt) => {
    const target = evt.target.closest('.otd-plan-symbol');
    if (!target) return;
    evt.preventDefault();
    selectSymbol(target.dataset.id);
    showContextMenu(evt.clientX, evt.clientY);
  });

  document.addEventListener('click', (evt) => {
    if (contextMenu?.contains(evt.target)) return;
    hideContextMenu();
  });

  canvas.addEventListener('pointerdown', handlePointerDown);
  canvas.addEventListener('pointermove', handlePointerMove);
  canvas.addEventListener('pointerup', handlePointerUp);

  canvas.addEventListener('dragover', (evt) => {
    if (!state.editMode) return;
    evt.preventDefault();
  });
  canvas.addEventListener('drop', handleDrop);

  function zoomTo(nextScale) {
    const clamped = Math.min(4, Math.max(0.4, nextScale));
    state.scale = clamped;
    applyGridVars();
  }

  zoomInBtn?.addEventListener('click', () => zoomTo(state.scale * 1.2));
  zoomOutBtn?.addEventListener('click', () => zoomTo(state.scale / 1.2));
  zoomResetBtn?.addEventListener('click', () => zoomTo(1));

  canvas.addEventListener('wheel', (evt) => {
    if (!evt.ctrlKey) return;
    evt.preventDefault();
    const factor = evt.deltaY < 0 ? 1.1 : 0.9;
    zoomTo(state.scale * factor);
  }, { passive: false });

  loadSymbolPalette();
  loadPlan();

  function canEditPlan() {
    const enabled = document.body?.dataset.authEnabled === 'true';
    const role = document.body?.dataset.authRole || '';
    return !enabled || role === 'admin';
  }

  function applyAuthState() {
    if (!menuEdit && !menuSave) return;
    const allowed = canEditPlan();
    if (menuEdit) {
      menuEdit.disabled = !allowed;
      menuEdit.classList.toggle('is-disabled', !allowed);
      menuEdit.title = allowed ? '' : 'Nur Admin';
    }
    if (menuSave) {
      menuSave.disabled = !allowed;
      menuSave.classList.toggle('is-disabled', !allowed);
      menuSave.title = allowed ? '' : 'Nur Admin';
    }
    if (!allowed && state.editMode) {
      setEditMode(false);
    }
  }

  document.addEventListener('otd-auth-change', applyAuthState);
  applyAuthState();
})();
