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
  const contextTest = document.getElementById('otd-plan-context-test');
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
    paletteFilter: '',
    dirty: false,
    saveTimer: null,
    isSaving: false,
    signalElements: {},
    signalLoaded: false
  };

  const configFields = [
    { key: 'name', label: 'Name', type: 'text' },
    { key: 'shortName', label: 'Kurzname', type: 'text' },
    { key: 'type', label: 'Typ', type: 'text' },
    { key: 'classes', label: 'CSS Klassen', type: 'text' },
    { key: 'file', label: 'Datei', type: 'text' },
    { key: 'trainNumber', label: 'Zugnummer', type: 'text' },
    { key: 'layer', label: 'Ebene', type: 'number' },
    { key: 'rotation', label: 'Rotation', type: 'select', options: ['0', '90', '180', '270'] },
    { key: 'sizeX', label: 'Breite Raster', type: 'number' },
    { key: 'sizeY', label: 'Hoehe Raster', type: 'number' },
    { key: 'direction', label: 'Richtung', type: 'text' },
    { key: 'address', label: 'addresse', type: 'text' },
    { key: 'protocol', label: 'Protokoll', type: 'text' },
    { key: 'length', label: 'Laenge', type: 'number' },
    { key: 'vmax', label: 'Vmax', type: 'number' },
    { key: 'vmin', label: 'Vmin', type: 'number' },
    { key: 'block', label: 'Block', type: 'text' },
    { key: 'section', label: 'Abschnitt', type: 'text' },
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

  function reportMelde(text, colorClass) {
    if (typeof window.addMeldeItem === 'function') {
      window.addMeldeItem(text, 'system', colorClass || 'otd-melde-item-gray');
    }
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
      if (!sym.config?.file && !(sym.classes || '').includes('otd-symbol--svg')) {
        continue;
      }
      const el = document.createElement('div');
      el.className = `otd-plan-symbol otd-symbol ${sym.classes || ''}`.trim();
      el.dataset.id = sym.id;
      const sizeX = Math.max(1, Number.parseInt(sym.config?.sizeX ?? '1', 10) || 1);
      const sizeY = Math.max(1, Number.parseInt(sym.config?.sizeY ?? '1', 10) || 1);
      el.style.width = `${state.gridSize * sizeX}px`;
      el.style.height = `${state.gridSize * sizeY}px`;
      el.style.left = `${(sym.x + sizeX / 2) * state.gridSize}px`;
      el.style.top = `${(sym.y + sizeY / 2) * state.gridSize}px`;
      const rotation = Number.parseFloat(sym.config?.rotation ?? '0');
      const rotationDeg = Number.isFinite(rotation) ? rotation : 0;
      el.style.transformOrigin = 'center';
      const rotationTransform = `translate(-50%, -50%) rotate(${rotationDeg}deg)`;
      if (sym.config?.file) {
        el.classList.add('otd-symbol--svg');
        const baseFile = normalizeSvgFile(sym.config.file);
        let renderFile = baseFile;
        if (sym.config?.testTurnout === 'diverge') {
          renderFile = toSwitchTurnoutGreen(baseFile);
        } else if (sym.config?.testTurnout === 'straight') {
          renderFile = toSwitchStraightGreen(baseFile);
        }
        if (sym.config?.testRoute === 'on') {
          renderFile = toTrackGreen(renderFile);
          renderFile = toNumberGreen(renderFile);
        }
        if (sym.config?.testSignal === 'green') {
          renderFile = toSignalGreen(renderFile);
        } else if (sym.config?.testSignal === 'red') {
          renderFile = toSignalRed(renderFile);
        }
        el.style.backgroundImage = `url("${renderFile}")`;
        el.style.backgroundRepeat = 'no-repeat';
        el.style.backgroundPosition = 'center';
        el.style.backgroundSize = '100% 100%';
        if (sym.config.file.includes('Zugnummernanzeiger')) {
          const label = document.createElement('span');
          label.className = 'otd-plan-symbol-number';
          label.textContent = (sym.config.trainNumber ?? '').toString().trim() || '8888';
          label.style.color = renderFile.includes('Zugnummernanzeiger_Green') ? '#2ecc71' : '#111';
          el.appendChild(label);
        }
      }
      el.style.transform = rotationTransform;
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

    function createFieldInput(field, value) {
      if (field.type === 'select' && Array.isArray(field.options)) {
        const select = document.createElement('select');
        field.options.forEach(optionValue => {
          const option = document.createElement('option');
          option.value = optionValue;
          option.textContent = optionValue;
          select.appendChild(option);
        });
        select.value = value?.toString() ?? field.options[0] ?? '';
        return select;
      }
      if (field.type === 'textarea') {
        const textarea = document.createElement('textarea');
        textarea.value = value ?? '';
        return textarea;
      }
      const input = document.createElement('input');
      input.type = field.type;
      input.value = value ?? '';
      return input;
    }

    for (const field of configFields) {
      const row = document.createElement('div');
      row.className = 'otd-plan-config-row';
      let value = sym.config[field.key] ?? (field.key === 'classes' ? sym.classes : '');
      if (field.key === 'trainNumber' && (sym.config?.file || '').includes('Zugnummernanzeiger')) {
        value = value?.toString().trim() || '8888';
        sym.config[field.key] = value;
      }
      const inputEl = createFieldInput(field, value);
      const handler = () => {
        if (field.key === 'classes') {
          sym.classes = inputEl.value.trim();
          sym.config[field.key] = inputEl.value.trim();
          render();
        } else if (field.key === 'rotation') {
          sym.config[field.key] = inputEl.value;
          render();
        } else {
          sym.config[field.key] = inputEl.value;
        }
        state.dirty = true;
        scheduleAutoSave();
      };
      inputEl.addEventListener('input', handler);
      inputEl.addEventListener('change', handler);
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
        state.dirty = true;
        scheduleAutoSave();
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
      let value = sym.config?.[field.key] ?? (field.key === 'classes' ? sym.classes : '');
      if (field.key === 'trainNumber' && (sym.config?.file || '').includes('Zugnummernanzeiger')) {
        value = value?.toString().trim() || '8888';
        if (!sym.config) sym.config = { extra: [] };
        sym.config[field.key] = value;
      }
      const inputEl = createFieldInput(field, value);
      const handler = () => {
        if (!sym.config) sym.config = { extra: [] };
        sym.config[field.key] = inputEl.value;
        if (field.key === 'classes') {
          sym.classes = inputEl.value.trim();
          render();
        }
        if (field.key === 'rotation') {
          render();
        }
        state.dirty = true;
        scheduleAutoSave();
      };
      inputEl.addEventListener('input', handler);
      inputEl.addEventListener('change', handler);
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
    const classes = payload.file ? 'otd-symbol--svg' : (payload.classes || '');
    const sizeX = Math.max(1, Number.parseInt(payload.sizeX ?? '1', 10) || 1);
    const sizeY = Math.max(1, Number.parseInt(payload.sizeY ?? '1', 10) || 1);
    const isNumberDisplay = (payload.file || '').includes('Zugnummernanzeiger');
    const sym = {
      id: crypto.randomUUID(),
      type: payload.type || '',
      classes,
      x: position.x,
      y: position.y,
      config: {
        name: payload.name || '',
        type: payload.type || '',
        classes,
        file: payload.file || '',
        trainNumber: isNumberDisplay ? '8888' : (payload.trainNumber || ''),
        sizeX,
        sizeY,
        extra: []
      }
    };
    state.symbols.push(sym);
    selectSymbol(sym.id);
    state.dirty = true;
    scheduleAutoSave();
    setStatus('Symbol hinzugefuegt');
  }

  function isSignalSymbol(sym) {
    const file = (sym?.config?.file || '').toLowerCase();
    return file.includes('.svg') && file.includes('signal');
  }

  function toSwitchStraight(file) {
    if (!file) return file;
    if (file.includes('Iltis_Switch_Straight.svg')) return file;
    if (file.includes('Iltis_Switch_Straight_White.svg')) return file;
    if (file.includes('Iltis_Switch_Straight_Green.svg')) {
      return file.replace('Iltis_Switch_Straight_Green.svg', 'Iltis_Switch_Straight_White.svg');
    }
    if (file.includes('Iltis_Switch_Turnout.svg')) {
      return file.replace('Iltis_Switch_Turnout.svg', 'Iltis_Switch_Straight.svg');
    }
    if (file.includes('Iltis_Switch_Turnout_White.svg')) {
      return file.replace('Iltis_Switch_Turnout_White.svg', 'Iltis_Switch_Straight_White.svg');
    }
    if (file.includes('Iltis_Switch_Turnout_Green.svg')) {
      return file.replace('Iltis_Switch_Turnout_Green.svg', 'Iltis_Switch_Straight_White.svg');
    }
    if (file.includes('Iltis_Switch_White.svg')) {
      return file.replace('Iltis_Switch_White.svg', 'Iltis_Switch_Straight_White.svg');
    }
    if (file.includes('Iltis_Switch.svg')) {
      return file.replace('Iltis_Switch.svg', 'Iltis_Switch_Straight_White.svg');
    }
    if (file.includes('Iltis_Turnout_Left.svg') || file.includes('Iltis_Turnout_Right.svg') || file.includes('Iltis_Turnout.svg')) {
      return file.replace(/Iltis_Turnout_(Left|Right)\.svg|Iltis_Turnout\.svg/g, 'Iltis_Switch_Straight.svg');
    }
    return file;
  }

  function normalizeSvgFile(file) {
    if (!file) return file;
    if (file.includes('Iltis_Zugnummernanzeiger.svg')) {
      return file.replace('Iltis_Zugnummernanzeiger.svg', 'Iltis_Zugnummernanzeiger_White.svg');
    }
    return file;
  }

  function toSwitchTurnout(file) {
    if (!file) return file;
    if (file.includes('Iltis_Switch_Turnout.svg')) return file;
    if (file.includes('Iltis_Switch_Turnout_White.svg')) return file;
    if (file.includes('Iltis_Switch_Turnout_Green.svg')) {
      return file.replace('Iltis_Switch_Turnout_Green.svg', 'Iltis_Switch_Turnout_White.svg');
    }
    if (file.includes('Iltis_Switch_Straight.svg')) {
      return file.replace('Iltis_Switch_Straight.svg', 'Iltis_Switch_Turnout.svg');
    }
    if (file.includes('Iltis_Switch_Straight_White.svg')) {
      return file.replace('Iltis_Switch_Straight_White.svg', 'Iltis_Switch_Turnout_White.svg');
    }
    if (file.includes('Iltis_Switch_Straight_Green.svg')) {
      return file.replace('Iltis_Switch_Straight_Green.svg', 'Iltis_Switch_Turnout_White.svg');
    }
    if (file.includes('Iltis_Switch_White.svg')) {
      return file.replace('Iltis_Switch_White.svg', 'Iltis_Switch_Turnout_White.svg');
    }
    if (file.includes('Iltis_Switch.svg')) {
      return file.replace('Iltis_Switch.svg', 'Iltis_Switch_Turnout_White.svg');
    }
    if (file.includes('Iltis_Turnout_Left.svg') || file.includes('Iltis_Turnout_Right.svg') || file.includes('Iltis_Turnout.svg')) {
      return file.replace(/Iltis_Turnout_(Left|Right)\.svg|Iltis_Turnout\.svg/g, 'Iltis_Switch_Turnout.svg');
    }
    return file;
  }

  function toSwitchStraightGreen(file) {
    const white = toSwitchStraight(file);
    if (white.includes('Iltis_Switch_Straight_White.svg')) {
      return white.replace('Iltis_Switch_Straight_White.svg', 'Iltis_Switch_Straight_Green.svg');
    }
    return white.replace('Iltis_Switch_Straight.svg', 'Iltis_Switch_Straight_Green.svg');
  }

  function toSwitchTurnoutGreen(file) {
    const white = toSwitchTurnout(file);
    if (white.includes('Iltis_Switch_Turnout_White.svg')) {
      return white.replace('Iltis_Switch_Turnout_White.svg', 'Iltis_Switch_Turnout_Green.svg');
    }
    return white.replace('Iltis_Switch_Turnout.svg', 'Iltis_Switch_Turnout_Green.svg');
  }

  function toTrackGreen(file) {
    if (!file) return file;
    if (file.includes('Iltis_Straight_Green.svg')) return file;
    if (file.includes('Iltis_Straight_White')) {
      return file.replace(/Iltis_Straight_White[^/]*\.svg/i, 'Iltis_Straight_Green.svg');
    }
    return file;
  }

  function toNumberGreen(file) {
    if (!file) return file;
    if (file.includes('Iltis_Zugnummernanzeiger_Green.svg')) return file;
    if (file.includes('Iltis_Zugnummernanzeiger_White.svg')) {
      return file.replace('Iltis_Zugnummernanzeiger_White.svg', 'Iltis_Zugnummernanzeiger_Green.svg');
    }
    return file;
  }
  function toSignalGreen(file) {
    if (!file) return file;
    if (file.includes('Iltis_Signal_Green.svg')) return file;
    if (file.includes('Iltis_Signal_Red.svg')) {
      return file.replace('Iltis_Signal_Red.svg', 'Iltis_Signal_Green.svg');
    }
    return file;
  }
  function toSignalRed(file) {
    if (!file) return file;
    if (file.includes('Iltis_Signal_Red.svg')) return file;
    if (file.includes('Iltis_Signal_Green.svg')) {
      return file.replace('Iltis_Signal_Green.svg', 'Iltis_Signal_Red.svg');
    }
    return file;
  }

  function isTurnoutSymbol(sym) {
    const text = `${sym?.type || ''} ${sym?.classes || ''} ${sym?.config?.type || ''} ${sym?.config?.file || ''}`.toLowerCase();
    return text.includes('turnout') || text.includes('weiche') || text.includes('switch');
  }

  function isTrackSymbol(sym) {
    const text = `${sym?.type || ''} ${sym?.classes || ''} ${sym?.config?.type || ''} ${sym?.config?.file || ''}`.toLowerCase();
    return text.includes('track') || text.includes('straight') || text.includes('gleis');
  }

  async function loadSignalElements() {
    try {
      const resp = await fetch('/elements.xml', { cache: 'no-store' });
      if (!resp.ok) {
        state.signalElements = {};
        state.signalLoaded = true;
        return;
      }
      const xml = await resp.text();
      const doc = new DOMParser().parseFromString(xml, 'application/xml');
      const nodes = doc.querySelectorAll('elements > signal');
      const map = {};
      nodes.forEach(node => {
        const id = node.getAttribute('id') || '';
        if (!id) return;
        map[id] = {
          id,
          address: node.getAttribute('address') ?? '',
          aspects: node.getAttribute('aspects') ?? '',
          asb: node.getAttribute('asb') ?? '',
          notes: node.getAttribute('notes') ?? ''
        };
      });
      state.signalElements = map;
      state.signalLoaded = true;
    } catch {
      state.signalElements = {};
      state.signalLoaded = true;
    }
  }

  async function saveSignalElements() {
    const payload = Object.values(state.signalElements);
    const resp = await fetch('/elements/save', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!resp.ok) {
      throw new Error(`HTTP ${resp.status}`);
    }
  }

  function ensureTestPanel() {
    let panel = document.getElementById('otd-test-panel');
    if (panel) return panel;
    panel = document.createElement('div');
    panel.id = 'otd-test-panel';
    panel.className = 'otd-test-panel otd-hidden';
    panel.innerHTML = `
      <div class="otd-test-panel-titlebar">
        <span>Test</span>
        <button type="button" class="otd-test-panel-close">X</button>
      </div>
      <div class="otd-test-panel-body" id="otd-test-panel-body"></div>
    `;
    document.body.appendChild(panel);
    panel.querySelector('.otd-test-panel-close')?.addEventListener('click', () => {
      panel.classList.add('otd-hidden');
    });
    return panel;
  }

  function setTestState(sym, updates) {
    if (!sym.config) sym.config = { extra: [] };
    Object.assign(sym.config, updates);
    render();
    state.dirty = true;
    scheduleAutoSave();
  }

  function openTestPanel(sym) {
    const panel = ensureTestPanel();
    const body = panel.querySelector('#otd-test-panel-body');
    body.innerHTML = '';

    if (isSignalSymbol(sym)) {
      body.innerHTML = `
        <button type="button" data-action="signal-red">Signal Rot</button>
        <button type="button" data-action="signal-green">Signal Gruen</button>
      `;
    } else if (isTurnoutSymbol(sym)) {
      body.innerHTML = `
        <button type="button" data-action="turnout-straight">Weiche Links</button>
        <button type="button" data-action="turnout-diverge">Weiche Rechts</button>
      `;
    } else if (isTrackSymbol(sym)) {
      body.innerHTML = `
        <button type="button" data-action="track-free">Gleis Frei</button>
        <button type="button" data-action="track-occupied">Gleis Belegt</button>
      `;
    } else {
      body.innerHTML = `
        <button type="button" data-action="highlight">Highlight</button>
        <button type="button" data-action="clear">Zuruecksetzen</button>
      `;
    }

    body.querySelectorAll('button').forEach((btn) => {
      btn.addEventListener('click', () => {
        const action = btn.dataset.action;
        if (action === 'signal-red') {
          setTestState(sym, { testSignal: 'red' });
        } else if (action === 'signal-green') {
          setTestState(sym, { testSignal: 'green' });
        } else if (action === 'turnout-straight') {
          setTestState(sym, { testTurnout: 'straight' });
        } else if (action === 'turnout-diverge') {
          setTestState(sym, { testTurnout: 'diverge' });
        } else if (action === 'track-free') {
          setTestState(sym, { testTrack: '' });
        } else if (action === 'track-occupied') {
          setTestState(sym, { testTrack: 'occupied' });
        } else if (action === 'highlight') {
          setTestState(sym, { testSignal: '', testTurnout: '', testTrack: '', testHighlight: 'on' });
        } else if (action === 'clear') {
          setTestState(sym, { testSignal: '', testTurnout: '', testTrack: '', testHighlight: '' });
        }
        panel.classList.add('otd-hidden');
      });
    });

    panel.classList.remove('otd-hidden');
  }

  function ensureSignalEditor() {
    let panel = document.getElementById('otd-signal-editor');
    if (panel) return panel;
    panel = document.createElement('div');
    panel.id = 'otd-signal-editor';
    panel.className = 'otd-signal-editor otd-hidden';
    panel.innerHTML = `
      <div class="otd-signal-editor-titlebar">
        <span>Signal bearbeiten</span>
        <button type="button" class="otd-signal-editor-close">X</button>
      </div>
      <div class="otd-signal-editor-body">
        <label>Adresse<input type="text" id="otd-signal-address" /></label>
        <label>Fahrbegriffe<input type="text" id="otd-signal-aspects" placeholder="z.B. Hp0, Hp1, Hp2" /></label>
        <label>ASB<input type="text" id="otd-signal-asb" /></label>
        <label>Notizen<textarea id="otd-signal-notes"></textarea></label>
        <div class="otd-signal-editor-actions">
          <button type="button" id="otd-signal-save">Speichern</button>
        </div>
      </div>
    `;
    document.body.appendChild(panel);
    panel.querySelector('.otd-signal-editor-close')?.addEventListener('click', () => {
      panel.classList.add('otd-hidden');
    });
    return panel;
  }

  async function openSignalEditor(sym) {
    if (!state.signalLoaded) {
      await loadSignalElements();
    }
    const panel = ensureSignalEditor();
    const entry = state.signalElements[sym.id] || { id: sym.id, address: '', aspects: '', asb: '', notes: '' };
    state.signalElements[sym.id] = entry;

    panel.querySelector('#otd-signal-address').value = entry.address ?? '';
    panel.querySelector('#otd-signal-aspects').value = entry.aspects ?? '';
    panel.querySelector('#otd-signal-asb').value = entry.asb ?? '';
    panel.querySelector('#otd-signal-notes').value = entry.notes ?? '';
    panel.classList.remove('otd-hidden');

    const saveBtn = panel.querySelector('#otd-signal-save');
    saveBtn.onclick = async () => {
      entry.address = panel.querySelector('#otd-signal-address').value.trim();
      entry.aspects = panel.querySelector('#otd-signal-aspects').value.trim();
      entry.asb = panel.querySelector('#otd-signal-asb').value.trim();
      entry.notes = panel.querySelector('#otd-signal-notes').value.trim();
      try {
        await saveSignalElements();
        setStatus('Signal gespeichert');
        panel.classList.add('otd-hidden');
      } catch {
        setStatus('Signal speichern fehlgeschlagen');
      }
    };
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
    state.dirty = true;
    scheduleAutoSave();
    setStatus('Dupliziert');
  }

  function deleteSelected() {
    if (!state.selectedId) return;
    state.symbols = state.symbols.filter(s => s.id !== state.selectedId);
    state.selectedId = null;
    render();
    renderConfig();
    state.dirty = true;
    scheduleAutoSave();
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

  function getSymbolSize(sym) {
    const sizeX = Math.max(1, Number.parseInt(sym.config?.sizeX ?? '1', 10) || 1);
    const sizeY = Math.max(1, Number.parseInt(sym.config?.sizeY ?? '1', 10) || 1);
    return { sizeX, sizeY };
  }

  function canPlaceSymbol(posX, posY, sizeX, sizeY, ignoreId) {
    const targetLeft = posX;
    const targetTop = posY;
    const targetRight = posX + sizeX - 1;
    const targetBottom = posY + sizeY - 1;

    for (const sym of state.symbols) {
      if (ignoreId && sym.id === ignoreId) continue;
      if (!sym.config?.file && !(sym.classes || '').includes('otd-symbol--svg')) {
        continue;
      }
      const { sizeX: otherX, sizeY: otherY } = getSymbolSize(sym);
      const otherLeft = sym.x;
      const otherTop = sym.y;
      const otherRight = sym.x + otherX - 1;
      const otherBottom = sym.y + otherY - 1;

      const overlaps =
        targetLeft <= otherRight &&
        targetRight >= otherLeft &&
        targetTop <= otherBottom &&
        targetBottom >= otherTop;
      if (overlaps) return false;
    }
    return true;
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
    const nextX = Math.max(0, state.drag.originX + dx);
    const nextY = Math.max(0, state.drag.originY + dy);
    const { sizeX, sizeY } = getSymbolSize(sym);
    if (!canPlaceSymbol(nextX, nextY, sizeX, sizeY, sym.id)) {
      return;
    }
    sym.x = nextX;
    sym.y = nextY;
    const el = canvas.querySelector(`[data-id="${sym.id}"]`);
    if (el) {
      el.style.left = `${(sym.x + sizeX / 2) * state.gridSize}px`;
      el.style.top = `${(sym.y + sizeY / 2) * state.gridSize}px`;
    }
  }

  function handlePointerUp(evt) {
    if (!state.drag) return;
    const sym = state.symbols.find(s => s.id === state.drag.id);
    if (sym) {
      const { sizeX, sizeY } = getSymbolSize(sym);
      if (!canPlaceSymbol(sym.x, sym.y, sizeX, sizeY, sym.id)) {
        sym.x = state.drag.originX;
        sym.y = state.drag.originY;
        render();
        setStatus('Platz belegt');
        state.drag = null;
        canvas.releasePointerCapture(evt.pointerId);
        return;
      }
    }
    state.drag = null;
    canvas.releasePointerCapture(evt.pointerId);
    state.dirty = true;
    scheduleAutoSave();
    setStatus('Position aktualisiert');
  }

  async function handleDrop(evt) {
    evt.preventDefault();
    if (!state.editMode) return;
    const raw = evt.dataTransfer?.getData('application/json');
    if (!raw) return;
    let payload;
    try {
      payload = JSON.parse(raw);
    } catch {
      return;
    }
    if (payload.file) {
      const size = await getSvgSizeUnits(payload.file);
      payload.sizeX = size.sizeX;
      payload.sizeY = size.sizeY;
    }
    const pos = getCanvasPosition(evt);
    const sizeX = Math.max(1, Number.parseInt(payload.sizeX ?? '1', 10) || 1);
    const sizeY = Math.max(1, Number.parseInt(payload.sizeY ?? '1', 10) || 1);
    if (!canPlaceSymbol(pos.x, pos.y, sizeX, sizeY)) {
      setStatus('Platz belegt');
      return;
    }
    addSymbol(payload, pos);
  }

  function serializeConfig(sym) {
    const fields = [];
    if (sym.config) {
      for (const [key, value] of Object.entries(sym.config)) {
        if (key === 'extra') continue;
        if (value !== undefined && value !== null && value !== '') {
          fields.push({ key, value: String(value) });
        }
      }
      if (Array.isArray(sym.config.extra)) {
        for (const extra of sym.config.extra) {
          if (extra.key) {
            fields.push({ key: String(extra.key), value: String(extra.value ?? '') });
          }
        }
      }
    }
    return fields;
  }

  async function savePlan() {
    if (state.isSaving) return;
    try {
      state.isSaving = true;
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
      state.dirty = false;
      setStatus('Gespeichert');
    } catch (err) {
      setStatus('Speichern fehlgeschlagen');
    } finally {
      state.isSaving = false;
    }
  }

  function parseConfigFields(node) {
    const config = { extra: [] };
    const fields = node.querySelectorAll('config > field');
      fields.forEach(field => {
        const key = field.getAttribute('key') || '';
        const value = field.getAttribute('value') || '';
        if (!key) return;
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
      state.dirty = false;
      render();
      renderConfig();
      setStatus(`Geladen (${symbols.length} Symbole)`);
    } catch (err) {
      setStatus('Laden fehlgeschlagen');
    }
  }

  async function loadSymbolPalette() {
    try {
      state.paletteItems = [];
      renderPalette();
      const resp = await fetch('/svg/list', { cache: 'no-store' });
      if (!resp.ok) throw new Error('svg list');
      const svgList = await resp.json();
      const svgItems = Array.isArray(svgList)
        ? svgList.map((file) => {
          const name = file.replace(/\.svg$/i, '');
          return {
            className: 'otd-symbol--svg',
            name,
            kind: 'svg',
            file: `/svg/${file}`
          };
        })
        : [];
      state.paletteItems = svgItems;
      renderPalette();
    } catch (err) {
      state.paletteItems = [];
      renderPalette();
      setStatus('Symbole nicht geladen');
    }
  }

  async function getSvgSizeUnits(fileUrl) {
    try {
      const resp = await fetch(fileUrl, { cache: 'no-store' });
      if (!resp.ok) throw new Error('svg');
      const text = await resp.text();
      const viewBoxMatch = text.match(/viewBox\s*=\s*["']([^"']+)["']/i);
      let width = null;
      let height = null;
      if (viewBoxMatch) {
        const parts = viewBoxMatch[1].trim().split(/\s+/);
        if (parts.length === 4) {
          width = Number.parseFloat(parts[2]);
          height = Number.parseFloat(parts[3]);
        }
      }
      if (!width || !height) {
        const widthMatch = text.match(/width\s*=\s*["']([\d.]+)/i);
        const heightMatch = text.match(/height\s*=\s*["']([\d.]+)/i);
        width = widthMatch ? Number.parseFloat(widthMatch[1]) : null;
        height = heightMatch ? Number.parseFloat(heightMatch[1]) : null;
      }
      if (!width || !height) {
        return { sizeX: 1, sizeY: 1 };
      }
      const ratio = width / height;
      if (ratio >= 2.4) return { sizeX: 3, sizeY: 1 };
      if (ratio >= 1.4) return { sizeX: 2, sizeY: 1 };
      return { sizeX: 1, sizeY: 1 };
    } catch {
      return { sizeX: 1, sizeY: 1 };
    }
  }

  function renderPalette() {
    if (!palette) return;
    palette.innerHTML = '';
    const filter = state.paletteFilter.toLowerCase();
    const list = state.paletteItems.filter(item => item.name.toLowerCase().includes(filter));
    const groups = new Map();
    for (const itemData of list) {
      const group = getPaletteGroup(itemData.name);
      if (!groups.has(group)) {
        groups.set(group, []);
      }
      groups.get(group).push(itemData);
    }

    for (const [groupName, items] of groups) {
      const groupEl = document.createElement('div');
      groupEl.className = 'otd-plan-palette-group';
      const title = document.createElement('div');
      title.className = 'otd-plan-palette-group-title';
      title.textContent = groupName;
      const grid = document.createElement('div');
      grid.className = 'otd-plan-palette-group-grid';

      for (const itemData of items) {
        const item = document.createElement('div');
        item.className = 'otd-plan-palette-item';
        item.dataset.class = itemData.className || '';
        item.draggable = state.editMode;
        const preview = document.createElement('span');
        preview.className = `otd-symbol ${itemData.className}`;
        if (itemData.kind === 'svg' && itemData.file) {
          preview.style.backgroundImage = `url("${itemData.file}")`;
          preview.style.backgroundSize = '100% 100%';
        }
        item.appendChild(preview);
        item.addEventListener('dragstart', (evt) => {
          if (!state.editMode) return;
          const payload = {
            classes: itemData.className || '',
            type: itemData.name,
            file: itemData.file || ''
          };
          evt.dataTransfer?.setData('application/json', JSON.stringify(payload));
          evt.dataTransfer?.setDragImage(item, 24, 24);
        });
        grid.appendChild(item);
      }

      groupEl.appendChild(title);
      groupEl.appendChild(grid);
      palette.appendChild(groupEl);
    }
  }

  function getPaletteGroup(name) {
    const text = (name || '').toLowerCase();
    if (text.includes('signal')) return 'Signal';
    if (text.includes('turnout') || text.includes('weiche')) return 'Turnout';
    if (text.includes('straight') || text.includes('track')) return 'Gleis';
    if (text.includes('zugnummer')) return 'Zugnummer';
    return 'Andere';
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
    const sym = getSelectedSymbol();
    if (sym && isSignalSymbol(sym)) {
      openSignalEditor(sym);
      hideContextMenu();
      return;
    }
    setEditMode(true);
    renderConfig();
    renderObjectWindow();
    hideContextMenu();
  });
  contextTest?.addEventListener('click', () => {
    const sym = getSelectedSymbol();
    if (!sym) return;
    openTestPanel(sym);
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

  function scheduleAutoSave() {
    if (!state.editMode) return;
    if (state.saveTimer) {
      clearTimeout(state.saveTimer);
    }
    state.saveTimer = window.setTimeout(() => {
      if (state.dirty) {
        savePlan();
      }
    }, 300);
  }

  function autoRefreshTick() {
    if (state.editMode || state.dirty || state.isSaving) return;
    loadPlan();
  }

  window.setInterval(autoRefreshTick, 2000);

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
