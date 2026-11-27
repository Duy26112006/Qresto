// JS dung chung cho man hinh nhan vien
// 1. Nhan thong bao SignalR va tai lai cac vung co data-tai-lai
// 2. Nut thao tac (form.js-action) gui bang fetch, xong thi tai lai danh sach
(function () {
    function toast(msg, loi) {
        const box = document.getElementById('toastBox');
        if (!box) { alert(msg); return; }
        const el = document.createElement('div');
        el.className = 'toast align-items-center border-0 text-white ' + (loi ? 'bg-danger' : 'bg-success');
        el.innerHTML = '<div class="d-flex"><div class="toast-body"></div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
        el.querySelector('.toast-body').textContent = msg;
        box.appendChild(el);
        const t = new bootstrap.Toast(el, { delay: loi ? 6000 : 2500 });
        t.show();
        el.addEventListener('hidden.bs.toast', () => el.remove());
    }
    window.qrestoToast = toast;

    // ===== tai lai cac vung danh sach =====
    const regions = () => document.querySelectorAll('[data-tai-lai]');
    let dangTai = false;
    let canTaiLai = false;

    async function reloadRegions() {
        if (dangTai) { canTaiLai = true; return; }
        dangTai = true;
        try {
            for (const r of regions()) {
                // dang go ly do trong o input thi khoan tai lai
                if (r.contains(document.activeElement) && document.activeElement.tagName === 'INPUT') continue;
                const res = await fetch(r.dataset.taiLai, { headers: { 'X-Requested-With': 'fetch' }, cache: 'no-store' });
                if (res.redirected || res.status === 401 || res.status === 403) {
                    location.reload(); // het phien dang nhap
                    return;
                }
                if (res.ok) r.innerHTML = await res.text();
            }
            const gio = document.getElementById('capNhatLuc');
            if (gio) gio.textContent = new Date().toLocaleTimeString('vi-VN');
        } catch (e) {
            // mat mang, lan sau tai lai
        } finally {
            dangTai = false;
            if (canTaiLai) { canTaiLai = false; reloadRegions(); }
        }
    }
    window.qrestoReload = reloadRegions;

    if (regions().length > 0) {
        reloadRegions();
        // du phong neu SignalR mat ket noi
        setInterval(reloadRegions, 30000);
    }

    // ===== SignalR =====
    const badge = document.getElementById('ketNoi');
    if (window.signalR) {
        const conn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/nhanvien')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        conn.on('capNhat', data => {
            if (data && data.loai === 'don-moi' && document.body.dataset.amThanh === '1') {
                beep();
            }
            reloadRegions();
        });

        function setBadge(text, cls) {
            if (!badge) return;
            badge.textContent = text;
            badge.className = 'badge ' + cls;
        }
        conn.onreconnecting(() => setBadge('Đang kết nối lại...', 'bg-warning text-dark'));
        conn.onreconnected(() => { setBadge('Trực tuyến', 'bg-success'); reloadRegions(); });
        conn.onclose(() => setBadge('Mất kết nối', 'bg-danger'));

        conn.start()
            .then(() => setBadge('Trực tuyến', 'bg-success'))
            .catch(() => setBadge('Mất kết nối', 'bg-danger'));
    }

    // tieng bip ngan khi co don moi (man hinh phuc vu)
    function beep() {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const o = ctx.createOscillator();
            o.frequency.value = 880;
            o.connect(ctx.destination);
            o.start();
            setTimeout(() => { o.stop(); ctx.close(); }, 200);
        } catch (e) { }
    }

    // ===== nut thao tac =====
    document.addEventListener('submit', async e => {
        const form = e.target;
        if (!form.classList.contains('js-action')) return;
        e.preventDefault();

        // nut can nhap ly do
        if (form.dataset.prompt) {
            const v = prompt(form.dataset.prompt, '');
            if (v === null) return;
            if (!v.trim()) {
                toast('Bạn chưa nhập nội dung.', true);
                return;
            }
            const field = form.querySelector('input[data-prompt-field]');
            if (field) field.value = v.trim();
        }
        if (form.dataset.confirm && !confirm(form.dataset.confirm)) return;

        const btns = form.querySelectorAll('button');
        btns.forEach(b => b.disabled = true);
        try {
            const res = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'fetch' }
            });
            const data = await res.json();
            toast(data.message, !data.ok);
        } catch (err) {
            toast('Không gửi được, kiểm tra kết nối rồi thử lại.', true);
        }
        btns.forEach(b => b.disabled = false);

        if (regions().length > 0) reloadRegions();
        else setTimeout(() => location.reload(), 1200); // de nguoi dung kip doc thong bao
    });

    // bat/tat am thanh
    const amThanh = document.getElementById('batAmThanh');
    if (amThanh) {
        amThanh.addEventListener('change', () => {
            document.body.dataset.amThanh = amThanh.checked ? '1' : '0';
            if (amThanh.checked) beep();
        });
    }
})();
