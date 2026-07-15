document.addEventListener('DOMContentLoaded', () => {
    // --- Mobile Menu Toggle ---
    const menuToggle = document.getElementById('menuToggle');
    const mainNav = document.getElementById('mainNav');

    if (menuToggle && mainNav) {
        menuToggle.addEventListener('click', () => {
            menuToggle.classList.toggle('active');
            mainNav.classList.toggle('active');
        });

        // Close menu when clicking navigation links
        const navLinks = mainNav.querySelectorAll('a');
        navLinks.forEach(link => {
            link.addEventListener('click', () => {
                menuToggle.classList.remove('active');
                mainNav.classList.remove('active');
            });
        });
    }

    // --- Scroll Fade In System ---
    const fadeItems = document.querySelectorAll('.fade-in');

    const observerOptions = {
        root: null,
        rootMargin: '0px',
        threshold: 0.15
    };

    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('active');
                // Once it is visible, stop observing
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    fadeItems.forEach(item => {
        observer.observe(item);
    });

    // Add immediate active state for hero section items on load
    setTimeout(() => {
        const heroItems = document.querySelectorAll('.hero-section .fade-in');
        heroItems.forEach(item => {
            item.classList.add('active');
        });
    }, 150);

    // --- Header Scrolled Effect ---
    const header = document.querySelector('.site-header');
    
    window.addEventListener('scroll', () => {
        if (window.scrollY > 20) {
            header.style.backgroundColor = 'rgba(10, 14, 23, 0.9)';
            header.style.padding = '8px 0';
            header.style.boxShadow = '0 10px 30px rgba(0, 0, 0, 0.3)';
        } else {
            header.style.backgroundColor = 'rgba(10, 14, 23, 0.7)';
            header.style.padding = '0';
            header.style.boxShadow = 'none';
        }
    });

    // --- Download Button Interaction ---
    const downloadBtns = document.querySelectorAll('.download-btn');
    downloadBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            // Optional: Create a subtle ripple/confetti effect or analytics trigger
            console.log('WinDLP installer download started.');
        });
    });
});
