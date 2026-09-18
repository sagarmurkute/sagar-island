/**
 * Sagar Island — Official Website Logic (Vanilla JS)
 * Interactive Live Dynamic Island Simulator, Copy Handlers, and Accordion
 */

document.addEventListener('DOMContentLoaded', () => {
    // -------------------------------------------------------------------------
    // 1. Dynamic Island Simulator Controller
    // -------------------------------------------------------------------------
    const island = document.getElementById('live-island');
    const simButtons = document.querySelectorAll('.sim-btn');
    const islandViews = document.querySelectorAll('.island-view');

    // Preset State Mapping
    const stateViews = {
        'resting': 'view-resting',
        'media-compact': 'view-media-compact',
        'media-expanded': 'view-media-expanded',
        'volume': 'view-volume',
        'brightness': 'view-brightness',
        'battery': 'view-battery',
        'clipboard': 'view-clipboard',
        'notification': 'view-notification'
    };

    function setIslandState(stateName) {
        if (!island || !stateViews[stateName]) return;

        // Set container data attribute for CSS spring dimension morphing
        island.setAttribute('data-state', stateName);

        // Hide all views, activate target view
        islandViews.forEach(v => v.classList.remove('active'));
        const targetView = document.getElementById(stateViews[stateName]);
        if (targetView) {
            targetView.classList.add('active');
        }

        // Update active button state
        simButtons.forEach(btn => {
            if (btn.getAttribute('data-state') === stateName) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
    }

    // Initialize Default State
    setIslandState('resting');

    // State Switcher Button Clicks
    simButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetState = btn.getAttribute('data-state');
            setIslandState(targetState);
        });
    });

    // Direct Island Click Interactions
    island.addEventListener('click', (e) => {
        // Prevent click if clicking child interactive controls inside expanded view
        if (e.target.closest('.ctrl-btn') || e.target.closest('.progress-track')) {
            return;
        }

        const currentState = island.getAttribute('data-state');
        if (currentState === 'resting') {
            setIslandState('media-compact');
        } else if (currentState === 'media-compact') {
            setIslandState('media-expanded');
        } else if (currentState === 'media-expanded') {
            setIslandState('resting');
        }
    });

    // -------------------------------------------------------------------------
    // 2. Simulated Media Player Progress & Controls
    // -------------------------------------------------------------------------
    const playBtn = document.getElementById('sim-play-btn');
    const timeCurrent = document.getElementById('sim-time-current');
    const progressTrack = document.getElementById('sim-progress-track');
    const progressFill = progressTrack ? progressTrack.querySelector('.progress-fill') : null;
    const progressThumb = progressTrack ? progressTrack.querySelector('.progress-thumb') : null;

    let isPlaying = true;
    let currentSeconds = 84; // 1:24
    const totalSeconds = 230; // 3:50

    function formatTime(secs) {
        const m = Math.floor(secs / 60);
        const s = Math.floor(secs % 60);
        return `${m}:${s < 10 ? '0' : ''}${s}`;
    }

    if (playBtn) {
        playBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            isPlaying = !isPlaying;
            if (isPlaying) {
                playBtn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="currentColor"><path d="M6 19h4V5H6v14zm8-14v14h4V5h-4z"/></svg>';
            } else {
                playBtn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="currentColor"><path d="M8 5v14l11-7z"/></svg>';
            }
        });
    }

    // Media playback timer loop
    setInterval(() => {
        if (isPlaying && island.getAttribute('data-state') === 'media-expanded') {
            currentSeconds++;
            if (currentSeconds > totalSeconds) currentSeconds = 0;

            if (timeCurrent) timeCurrent.textContent = formatTime(currentSeconds);
            const pct = (currentSeconds / totalSeconds) * 100;
            if (progressFill) progressFill.style.width = `${pct}%`;
            if (progressThumb) progressThumb.style.left = `${pct}%`;
        }
    }, 1000);

    // -------------------------------------------------------------------------
    // 3. Simulated Volume Scroll Wheel Control
    // -------------------------------------------------------------------------
    let simVolume = 72;
    const volumeFill = document.getElementById('volume-fill');
    const volumeValue = document.getElementById('volume-value');

    island.addEventListener('wheel', (e) => {
        const currentState = island.getAttribute('data-state');
        if (currentState === 'volume') {
            e.preventDefault();
            if (e.deltaY < 0) {
                simVolume = Math.min(100, simVolume + 2);
            } else {
                simVolume = Math.max(0, simVolume - 2);
            }
            if (volumeFill) volumeFill.style.width = `${simVolume}%`;
            if (volumeValue) volumeValue.textContent = `${simVolume}%`;
        }
    }, { passive: false });

    // -------------------------------------------------------------------------
    // 4. One-Click Copy to Clipboard Handlers
    // -------------------------------------------------------------------------
    function copyTextToClipboard(text, triggerEl) {
        navigator.clipboard.writeText(text).then(() => {
            // Visual feedback
            const copyIcon = triggerEl.querySelector('.copy-icon');
            const checkIcon = triggerEl.querySelector('.check-icon');

            if (copyIcon && checkIcon) {
                copyIcon.style.display = 'none';
                checkIcon.style.display = 'inline-block';
                setTimeout(() => {
                    copyIcon.style.display = 'inline-block';
                    checkIcon.style.display = 'none';
                }, 2000);
            } else {
                const originalHtml = triggerEl.innerHTML;
                triggerEl.innerHTML = '<span style="color:#10b981;font-size:12px;font-weight:700;">✓ Copied!</span>';
                setTimeout(() => {
                    triggerEl.innerHTML = originalHtml;
                }, 2000);
            }
        }).catch(err => {
            console.error('Failed to copy text: ', err);
        });
    }

    // Hero Quick Command Box Copy
    const heroCopyBtn = document.getElementById('copy-quick-cmd');
    const heroCmdText = document.getElementById('quick-cmd-text');
    if (heroCopyBtn && heroCmdText) {
        heroCopyBtn.addEventListener('click', () => {
            copyTextToClipboard(heroCmdText.textContent.trim(), heroCopyBtn);
        });
    }

    // Install Section Copy Buttons
    const genericCopyBtns = document.querySelectorAll('.copy-btn');
    genericCopyBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const textToCopy = btn.getAttribute('data-copy');
            if (textToCopy) {
                copyTextToClipboard(textToCopy, btn);
            }
        });
    });

    // -------------------------------------------------------------------------
    // 5. FAQ Accordion Interaction
    // -------------------------------------------------------------------------
    const faqItems = document.querySelectorAll('.faq-item');
    faqItems.forEach(item => {
        const questionBtn = item.querySelector('.faq-question');
        if (questionBtn) {
            questionBtn.addEventListener('click', () => {
                const isActive = item.classList.contains('active');
                faqItems.forEach(i => i.classList.remove('active'));
                if (!isActive) {
                    item.classList.add('active');
                }
            });
        }
    });

    // -------------------------------------------------------------------------
    // 6. Navigation Background Blur on Scroll
    // -------------------------------------------------------------------------
    const navbar = document.getElementById('navbar');
    window.addEventListener('scroll', () => {
        if (window.scrollY > 40) {
            navbar.style.background = 'rgba(6, 7, 10, 0.9)';
            navbar.style.borderBottomColor = 'rgba(255, 255, 255, 0.14)';
        } else {
            navbar.style.background = 'rgba(6, 7, 10, 0.75)';
            navbar.style.borderBottomColor = 'rgba(255, 255, 255, 0.08)';
        }
    });
});
