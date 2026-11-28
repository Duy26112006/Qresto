// Trang goi mon cua khach
// - Gio hang luu trong localStorage cua trinh duyet, chua gui thi nhan vien khong thay
// - Moi 5 giay lay trang thai mon cua ban tu server
(function () {
    const app = document.getElementById('app');
    if (!app) return;

    const token = app.dataset.token;
    let sessionId = app.dataset.session ? parseInt(app.dataset.session) : null;
    const turn = parseInt(app.dataset.turn);
    const url = {
        status: app.dataset.urlStatus,
        send: app.dataset.urlSend,
        cancel: app.dataset.urlCancel,
        support: app.dataset.urlSupport,
        pay: app.dataset.urlPay
    };

    const MAX_QTY = 50;
    const MAX_LINES = 50;
    const gioKey = 'qresto_gio_' + token;

    let stale = false;          // trang thuoc luot khach cu
    let canOrder = true;        // ban dang phuc vu hoac chua co phien
    let dangGui = false;
    let lastStatus = null;

    const gioSheet = new bootstrap.Offcanvas(document.getElementById('gioHang'));
    const daGoiSheet = new bootstrap.Offcanvas(document.getElementById('monDaGoi'));
    const huyModal = new bootstrap.Modal(document.getElementById('huyModal'));

    // ========== tien ich ==========

    function vnd(n) {
        return (n || 0).toLocaleString('vi-VN') + ' đ';
    }

    function boDau(s) {
        return (s || '').normalize('NFD').replace(/[̀-ͯ]/g, '')
            .replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase();
    }

    function escapeHtml(s) {
        const div = document.createElement('div');
        div.textContent = s == null ? '' : s;
        return div.innerHTML;
    }

    // crypto.randomUUID chi co tren https, truy cap qua IP trong mang LAN thi khong co
    function newRequestId() {
        const bytes = new Uint8Array(16);
        if (window.crypto && window.crypto.getRandomValues) {
            window.crypto.getRandomValues(bytes);
        } else {
            for (let i = 0; i < 16; i++) bytes[i] = Math.floor(Math.random() * 256);
        }
        return Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
    }

    let toastTimer = null;
    function toast(msg, loi) {
        const el = document.getElementById('khToast');
        el.textContent = msg;
        el.className = 'kh-toast show' + (loi ? ' loi' : '');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => el.className = 'kh-toast', 3500);
    }

    async function postJson(u, body) {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const res = await fetch(u, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': tokenInput ? tokenInput.value : ''
            },
            body: JSON.stringify(body)
        });
        let data = null;
        try { data = await res.json(); } catch (e) { data = null; }
        if (!data) data = { ok: false, code: 'Error', message: 'Có lỗi xảy ra, vui lòng thử lại.' };
        return data;
    }

    // ========== gio hang ==========

    function loadCart() {
        try {
            const c = JSON.parse(localStorage.getItem(gioKey));
            if (c && Array.isArray(c.lines)) return c;
        } catch (e) { }
        return { lines: [], pending: null };
    }

    let cart = loadCart();

    function saveCart() {
        try {
            localStorage.setItem(gioKey, JSON.stringify(cart));
        } catch (e) {
            // che do an danh co the khong cho luu, gio van dung duoc tren trang hien tai
        }
    }

    function findLine(id) {
        return cart.lines.find(l => l.id === id);
    }

    function qtyOf(id) {
        const l = findLine(id);
        return l ? l.qty : 0;
    }

    function setQty(id, qty, name, price) {
        let line = findLine(id);
        if (qty <= 0) {
            cart.lines = cart.lines.filter(l => l.id !== id);
        } else {
            if (qty > MAX_QTY) {
                toast('Mỗi món tối đa ' + MAX_QTY + ' phần.', true);
                qty = MAX_QTY;
            }
            if (!line) {
                if (cart.lines.length >= MAX_LINES) {
                    toast('Mỗi đơn tối đa ' + MAX_LINES + ' món.', true);
                    return;
                }
                line = { id: id, name: name, price: price, qty: 0, note: '' };
                cart.lines.push(line);
            }
            line.qty = qty;
            delete line.loi;
        }
        saveCart();
        renderAll();
    }

    function cartCount() {
        return cart.lines.reduce((s, l) => s + l.qty, 0);
    }

    function cartTotal() {
        return cart.lines.reduce((s, l) => s + l.qty * l.price, 0);
    }

    // ========== hien thi ==========

    function renderCardButtons() {
        document.querySelectorAll('.mon').forEach(card => {
            const id = parseInt(card.dataset.id);
            const box = card.querySelector('.mon-nut');
            const q = qtyOf(id);
            let html;
            if (card.dataset.status === 'TamHet') {
                html = '<span class="badge bg-light text-muted border">Tạm hết</span>';
            } else if (q === 0) {
                html = '<button type="button" class="btn btn-sm btn-outline-qresto nut-them">Thêm +</button>';
            } else {
                html = '<div class="so-luong">' +
                    '<button type="button" class="nut-giam">−</button>' +
                    '<span>' + q + '</span>' +
                    '<button type="button" class="nut-tang">+</button></div>';
            }
            if (box.innerHTML !== html) box.innerHTML = html;
        });
    }

    function renderBar() {
        const n = cartCount();
        document.getElementById('soPhan').textContent = n;
        document.getElementById('tongGio').textContent = vnd(cartTotal());
        document.getElementById('tongGio2').textContent = vnd(cartTotal());
        document.getElementById('thanhGio').classList.toggle('d-none', n === 0);
        document.body.classList.toggle('co-gio', n > 0);
    }

    function renderCart() {
        const box = document.getElementById('dsGio');
        if (cart.lines.length === 0) {
            box.innerHTML = '<p class="text-muted small">Giỏ đang trống.</p>';
            return;
        }
        box.innerHTML = cart.lines.map(l => `
            <div class="dong-gio ${l.loi ? 'co-loi' : ''}" data-id="${l.id}">
                <div class="d-flex justify-content-between align-items-start">
                    <div class="me-2">
                        <div class="fw-semibold">${escapeHtml(l.name)}</div>
                        <div class="small text-muted">${vnd(l.price)}</div>
                        ${l.loi ? `<div class="small text-danger">${escapeHtml(l.loi)}</div>` : ''}
                    </div>
                    <div class="so-luong">
                        <button type="button" class="nut-giam">−</button>
                        <span>${l.qty}</span>
                        <button type="button" class="nut-tang">+</button>
                    </div>
                </div>
                <input type="text" class="form-control form-control-sm mt-1 ghi-chu" maxlength="300"
                       placeholder="Ghi chú (vd: ít cay, không hành)" value="${escapeHtml(l.note)}" />
            </div>`).join('');
    }

    function renderAll() {
        renderCardButtons();
        renderBar();
        renderCart();
        updateSendButton();
    }

    function updateSendButton() {
        const btn = document.getElementById('btnGuiDon');
        if (stale) {
            btn.disabled = true;
            btn.textContent = 'Lượt phục vụ đã kết thúc';
        } else if (!canOrder) {
            btn.disabled = true;
            btn.textContent = 'Bàn đang chờ thanh toán';
        } else {
            btn.disabled = dangGui || cart.lines.length === 0;
            btn.textContent = dangGui ? 'Đang gửi...' : 'Gửi đơn';
        }
    }

    // ========== su kien thuc don ==========

    document.getElementById('thucDon').addEventListener('click', e => {
        const card = e.target.closest('.mon');
        if (!card || card.dataset.status === 'TamHet') return;
        const id = parseInt(card.dataset.id);
        const name = card.dataset.name;
        const price = parseInt(card.dataset.price);
        if (e.target.closest('.nut-them') || e.target.closest('.nut-tang')) {
            setQty(id, qtyOf(id) + 1, name, price);
        } else if (e.target.closest('.nut-giam')) {
            setQty(id, qtyOf(id) - 1, name, price);
        }
    });

    document.getElementById('dsGio').addEventListener('click', e => {
        const row = e.target.closest('.dong-gio');
        if (!row) return;
        const id = parseInt(row.dataset.id);
        const line = findLine(id);
        if (!line) return;
        if (e.target.closest('.nut-tang')) setQty(id, line.qty + 1);
        else if (e.target.closest('.nut-giam')) setQty(id, line.qty - 1);
    });

    // ghi chu luu khi go, khong render lai de khong mat con tro
    document.getElementById('dsGio').addEventListener('input', e => {
        if (!e.target.classList.contains('ghi-chu')) return;
        const id = parseInt(e.target.closest('.dong-gio').dataset.id);
        const line = findLine(id);
        if (line) {
            line.note = e.target.value.slice(0, 300);
            saveCart();
        }
    });

    document.getElementById('btnXemGio').addEventListener('click', () => {
        document.getElementById('loiGio').textContent = '';
        renderCart();
        gioSheet.show();
    });

    // loc danh muc
    document.getElementById('danhMuc').addEventListener('click', e => {
        const chip = e.target.closest('.chip');
        if (!chip) return;
        document.querySelectorAll('#danhMuc .chip').forEach(c => c.classList.toggle('active', c === chip));
        applyFilter();
    });

    document.getElementById('timMon').addEventListener('input', applyFilter);

    function applyFilter() {
        const cat = document.querySelector('#danhMuc .chip.active').dataset.cat;
        const q = boDau(document.getElementById('timMon').value.trim());
        let count = 0;
        document.querySelectorAll('.nhom-mon').forEach(sec => {
            let visibleInSection = 0;
            const catOk = !cat || sec.dataset.cat === cat;
            sec.querySelectorAll('.mon').forEach(card => {
                const show = catOk && (!q || boDau(card.dataset.name).includes(q));
                card.classList.toggle('d-none', !show);
                if (show) visibleInSection++;
            });
            sec.classList.toggle('d-none', visibleInSection === 0);
            count += visibleInSection;
        });
        document.getElementById('khongThayMon').classList.toggle('d-none', count > 0);
    }

    // ========== gui don ==========

    function cartSignature() {
        return JSON.stringify(cart.lines.map(l => [l.id, l.qty, (l.note || '').trim(), l.price]));
    }

    document.getElementById('btnGuiDon').addEventListener('click', async () => {
        if (dangGui || cart.lines.length === 0 || stale || !canOrder) return;
        const loiBox = document.getElementById('loiGio');
        loiBox.textContent = '';

        // Cung noi dung thi dung lai ma cu (gui lai sau khi mat mang), doi gio thi sinh ma moi
        const sig = cartSignature();
        if (!cart.pending || cart.pending.sig !== sig) {
            cart.pending = { rid: newRequestId(), sig: sig };
            saveCart();
        }

        const body = {
            clientRequestId: cart.pending.rid,
            pageSessionId: sessionId,
            pageTurn: turn,
            items: cart.lines.map(l => ({ menuItemId: l.id, quantity: l.qty, note: l.note, price: l.price }))
        };

        dangGui = true;
        updateSendButton();
        let res;
        try {
            res = await postJson(url.send, body);
        } catch (e) {
            dangGui = false;
            updateSendButton();
            loiBox.textContent = 'Mất kết nối, giỏ vẫn được giữ. Bấm Gửi đơn lại khi có mạng (đơn sẽ không bị gửi trùng).';
            return;
        }
        dangGui = false;

        if (res.ok) {
            sessionId = res.data.sessionId;
            cart = { lines: [], pending: null };
            saveCart();
            renderAll();
            gioSheet.hide();
            toast(res.data.duplicate ? 'Đơn này đã được gửi trước đó.' : 'Đã gửi đơn! Nhân viên sẽ xác nhận món ngay.');
            refreshStatus();
            return;
        }

        switch (res.code) {
            case 'ItemUnavailable':
                (res.data && res.data.items || []).forEach(id => {
                    const l = findLine(id);
                    if (l) l.loi = 'Món này vừa hết, vui lòng bỏ ra khỏi giỏ.';
                });
                break;
            case 'PriceChanged':
                (res.data && res.data.items || []).forEach(x => {
                    const l = findLine(x.menuItemId);
                    if (l) {
                        l.loi = 'Giá đổi từ ' + vnd(x.oldPrice) + ' thành ' + vnd(x.newPrice);
                        l.price = x.newPrice;
                    }
                    const card = document.querySelector('.mon[data-id="' + x.menuItemId + '"]');
                    if (card) {
                        card.dataset.price = x.newPrice;
                        card.querySelector('.mon-gia').textContent = vnd(x.newPrice);
                    }
                });
                break;
            case 'StaleSession':
                setStale(res.message);
                break;
            case 'SessionClosed':
                canOrder = false;
                break;
        }
        saveCart();
        renderAll();
        loiBox.textContent = res.message;
        refreshStatus();
    });

    // ========== trang thai ban ==========

    function setStale(msg) {
        stale = true;
        canOrder = false;
        const tb = document.getElementById('thongBaoBan');
        tb.textContent = msg || 'Lượt phục vụ này đã kết thúc. Quét lại mã QR trên bàn để gọi món.';
        tb.classList.remove('d-none');
        updateSendButton();
        document.getElementById('btnThanhToan').disabled = true;
        document.querySelectorAll('[data-ho-tro]').forEach(b => b.disabled = true);
    }

    async function refreshStatus() {
        if (stale) return;
        let s;
        try {
            const q = '?turn=' + turn + (sessionId ? '&sessionId=' + sessionId : '');
            const res = await fetch(url.status + q, { cache: 'no-store' });
            s = await res.json();
        } catch (e) {
            return; // mat mang tam thoi, lan sau lay lai
        }
        lastStatus = s;

        if (s.stale) {
            setStale(s.message);
            return;
        }
        if (s.sessionId) sessionId = s.sessionId;
        canOrder = s.canOrder;

        // cap nhat tinh trang ban cua tung mon
        (s.menu || []).forEach(m => {
            const card = document.querySelector('.mon[data-id="' + m.id + '"]');
            if (!card) return;
            const st = m.status === 'NgungBan' ? 'TamHet' : m.status;
            card.dataset.status = st;
            card.classList.toggle('het', st === 'TamHet');
            if (parseInt(card.dataset.price) !== m.price) {
                card.dataset.price = m.price;
                card.querySelector('.mon-gia').textContent = vnd(m.price);
            }
        });

        const tb = document.getElementById('thongBaoBan');
        const ttBan = document.getElementById('trangThaiBan');
        if (s.sessionStatus === 'ChoThanhToan') {
            tb.textContent = 'Bàn đang chờ thanh toán nên tạm dừng gọi món. Muốn gọi thêm, hãy gọi nhân viên.';
            tb.classList.remove('d-none');
            ttBan.textContent = 'Đang chờ thanh toán';
        } else {
            tb.classList.add('d-none');
            ttBan.textContent = s.sessionId ? 'Đang phục vụ' : 'Chào mừng bạn!';
        }

        document.getElementById('btnThanhToan').disabled = !s.sessionId || s.sessionStatus !== 'DangPhucVu';
        document.getElementById('btnThanhToan').textContent =
            s.sessionStatus === 'ChoThanhToan' ? 'Đã yêu cầu thanh toán' : 'Yêu cầu thanh toán';
        document.querySelectorAll('[data-ho-tro]').forEach(b => {
            const open = (s.openSupport || []).includes(b.dataset.hoTro);
            b.disabled = !s.sessionId || open;
            b.title = open ? 'Nhân viên đang xử lý yêu cầu trước' : '';
        });

        renderOrdered(s);
        renderAll();
    }

    function renderOrdered(s) {
        const items = s.items || [];
        const dangCho = items.filter(i => i.status !== 'DaHuy' && i.status !== 'BiTuChoi').length;
        document.getElementById('soMonDaGoi').textContent = dangCho;
        document.getElementById('tamTinh').textContent = vnd(s.subtotal);
        document.getElementById('choXacNhan').textContent =
            s.pendingTotal > 0 ? 'Chờ nhân viên xác nhận: ' + vnd(s.pendingTotal) + ' (chưa cộng vào tạm tính)' : '';

        const box = document.getElementById('dsDaGoi');
        if (items.length === 0) {
            box.innerHTML = '<p class="text-muted small">Bàn chưa gọi món nào.</p>';
            return;
        }
        box.innerHTML = items.map(i => `
            <div class="dong-da-goi ${i.status === 'DaHuy' || i.status === 'BiTuChoi' ? 'gach' : ''}">
                <div class="d-flex justify-content-between">
                    <div>
                        <span class="fw-semibold">${i.quantity} × ${escapeHtml(i.name)}</span>
                        ${i.mine ? '<span class="badge bg-light text-dark border ms-1">bạn gọi</span>' : ''}
                        ${i.note ? `<div class="small text-muted">Ghi chú: ${escapeHtml(i.note)}</div>` : ''}
                        ${i.reason ? `<div class="small text-danger">${escapeHtml(i.reason)}</div>` : ''}
                        ${i.cancelRequestStatus ? `<div class="small text-muted">Yêu cầu hủy: ${escapeHtml(i.cancelRequestStatus)}</div>` : ''}
                    </div>
                    <div class="text-end">
                        <span class="trang-thai tt-${i.status}">${escapeHtml(i.statusText)}</span>
                        <div class="small text-muted">${i.time}</div>
                        ${i.canRequestCancel ? `<button type="button" class="btn btn-link btn-sm p-0 text-danger nut-huy" data-id="${i.id}" data-name="${escapeHtml(i.name)}">Yêu cầu hủy</button>` : ''}
                    </div>
                </div>
            </div>`).join('');
    }

    document.getElementById('btnMonDaGoi').addEventListener('click', () => {
        refreshStatus();
        daGoiSheet.show();
    });

    // ========== yeu cau huy ==========

    let huyItemId = null;
    document.getElementById('dsDaGoi').addEventListener('click', e => {
        const btn = e.target.closest('.nut-huy');
        if (!btn) return;
        huyItemId = parseInt(btn.dataset.id);
        document.getElementById('huyTenMon').textContent = btn.dataset.name;
        document.getElementById('huyLyDo').value = '';
        huyModal.show();
    });

    document.getElementById('btnGuiHuy').addEventListener('click', async () => {
        const reason = document.getElementById('huyLyDo').value.trim();
        if (!reason) {
            toast('Vui lòng nhập lý do hủy.', true);
            return;
        }
        try {
            const res = await postJson(url.cancel, { sessionId: sessionId, orderItemId: huyItemId, reason: reason });
            toast(res.message || (res.ok ? res.data : ''), !res.ok);
            if (res.ok) huyModal.hide();
            if (res.code === 'StaleSession') setStale(res.message);
        } catch (e) {
            toast('Mất kết nối, vui lòng thử lại.', true);
        }
        refreshStatus();
    });

    // ========== ho tro va thanh toan ==========

    document.querySelectorAll('[data-ho-tro]').forEach(btn => {
        btn.addEventListener('click', async () => {
            if (!sessionId) return;
            btn.disabled = true;
            try {
                const res = await postJson(url.support, { sessionId: sessionId, type: btn.dataset.hoTro, note: null });
                toast(res.ok ? res.data : res.message, !res.ok);
                if (res.code === 'StaleSession') setStale(res.message);
            } catch (e) {
                toast('Mất kết nối, vui lòng thử lại.', true);
            }
            refreshStatus();
        });
    });

    document.getElementById('btnThanhToan').addEventListener('click', async () => {
        if (!sessionId) return;
        if (!confirm('Yêu cầu thanh toán? Sau khi gửi, bàn sẽ tạm dừng gọi thêm món.')) return;
        try {
            const res = await postJson(url.pay, { sessionId: sessionId });
            toast(res.ok ? res.data : res.message, !res.ok);
            if (res.code === 'StaleSession') setStale(res.message);
        } catch (e) {
            toast('Mất kết nối, vui lòng thử lại.', true);
        }
        refreshStatus();
    });

    // ========== khoi dong ==========

    renderAll();
    refreshStatus();
    setInterval(refreshStatus, 5000);
    // quay lai tab thi cap nhat ngay
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) refreshStatus();
    });
})();
