// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', function () {
    // Check if GSAP is loaded
    if (typeof gsap !== 'undefined') {
        // Register ScrollTrigger if available
        if (typeof ScrollTrigger !== 'undefined') {
            gsap.registerPlugin(ScrollTrigger);
        }

        // Global Fade In
        gsap.utils.toArray('.gsap-fade-in').forEach(function(elem) {
            gsap.fromTo(elem, 
                { opacity: 0 }, 
                { 
                    opacity: 1, 
                    duration: 1, 
                    ease: "power2.out",
                    scrollTrigger: {
                        trigger: elem,
                        start: "top 85%",
                        toggleActions: "play none none none"
                    }
                }
            );
        });

        // Global Slide Up
        gsap.utils.toArray('.gsap-slide-up').forEach(function(elem) {
            gsap.fromTo(elem, 
                { opacity: 0, y: 50 }, 
                { 
                    opacity: 1, 
                    y: 0, 
                    duration: 0.8, 
                    ease: "power3.out",
                    scrollTrigger: {
                        trigger: elem,
                        start: "top 85%",
                        toggleActions: "play none none none"
                    }
                }
            );
        });

        // Staggered lists/cards
        const staggerContainers = document.querySelectorAll('.gsap-stagger-container');
        staggerContainers.forEach(container => {
            const items = container.querySelectorAll('.gsap-stagger-item');
            if(items.length > 0) {
                gsap.fromTo(items,
                    { opacity: 0, y: 30 },
                    {
                        opacity: 1,
                        y: 0,
                        duration: 0.6,
                        stagger: 0.1,
                        ease: "power2.out",
                        scrollTrigger: {
                            trigger: container,
                            start: "top 85%"
                        }
                    }
                );
            }
        });
        
        // Pulse animation for buttons or icons
        gsap.utils.toArray('.gsap-pulse').forEach(function(elem) {
            elem.addEventListener('mouseenter', () => {
                gsap.to(elem, { scale: 1.05, duration: 0.2, ease: "power1.inOut" });
            });
            elem.addEventListener('mouseleave', () => {
                gsap.to(elem, { scale: 1, duration: 0.2, ease: "power1.inOut" });
            });
        });

        // ==========================================
        // NEW ADVANCED PREMIUM WEB ANIMATIONS
        // ==========================================

        // 1. Sidebar Links Staggered Entrance on Load
        const sidebarLinks = document.querySelectorAll('.sidebar .nav-link, .sidebar .text-uppercase');
        if (sidebarLinks.length > 0) {
            gsap.fromTo(sidebarLinks,
                { opacity: 0, x: -20 },
                { opacity: 1, x: 0, duration: 0.5, stagger: 0.03, ease: "power2.out" }
            );
        }

        // 2. Sidebar Logo back-out scale on hover
        const sidebarLogo = document.querySelector('.sidebar img');
        if (sidebarLogo) {
            sidebarLogo.addEventListener('mouseenter', () => {
                gsap.to(sidebarLogo, { scale: 1.05, duration: 0.3, ease: "back.out(1.7)" });
            });
            sidebarLogo.addEventListener('mouseleave', () => {
                gsap.to(sidebarLogo, { scale: 1, duration: 0.3, ease: "power2.out" });
            });
        }

        // 3. Card Hover (Glow & Subtle lift)
        gsap.utils.toArray('.card').forEach(function(card) {
            card.addEventListener('mouseenter', () => {
                gsap.to(card, { y: -6, boxShadow: "0 10px 20px rgba(0, 0, 0, 0.08)", duration: 0.3, ease: "power2.out" });
            });
            card.addEventListener('mouseleave', () => {
                gsap.to(card, { y: 0, boxShadow: "0 2px 4px rgba(0, 0, 0, 0.04)", duration: 0.3, ease: "power2.out" });
            });
        });

        // 4. Staggered Table Rows fade-in and slide-up
        document.querySelectorAll('.table').forEach(table => {
            const rows = table.querySelectorAll('tbody tr');
            if (rows.length > 0) {
                gsap.fromTo(rows,
                    { opacity: 0, y: 15 },
                    { 
                        opacity: 1, 
                        y: 0, 
                        duration: 0.4, 
                        stagger: 0.025, 
                        ease: "power2.out",
                        scrollTrigger: {
                            trigger: table,
                            start: "top 95%"
                        }
                    }
                );
            }
        });

        // 5. Statistics Roll-Up Count Animation
        const metricCounts = document.querySelectorAll('.metric-count');
        metricCounts.forEach(elem => {
            const originalText = elem.innerText.trim();
            const hasCurrency = originalText.includes('₱') || originalText.includes('$') || originalText.charCodeAt(0) === 8369;
            const currencySymbol = hasCurrency ? (originalText.includes('₱') || originalText.charCodeAt(0) === 8369 ? '₱' : '$') : '';
            
            const rawNumber = parseFloat(originalText.replace(/[^0-9.-]/g, '')) || 0;
            
            if (!isNaN(rawNumber) && rawNumber > 0) {
                let obj = { val: 0 };
                gsap.to(obj, {
                    val: rawNumber,
                    duration: 1.5,
                    ease: "power2.out",
                    scrollTrigger: {
                        trigger: elem,
                        start: "top 95%"
                    },
                    onUpdate: function() {
                        if (hasCurrency) {
                            elem.innerText = currencySymbol + Math.floor(obj.val).toLocaleString();
                        } else if (originalText.endsWith('%')) {
                            elem.innerText = obj.val.toFixed(1) + '%';
                        } else {
                            elem.innerText = Math.floor(obj.val).toLocaleString();
                        }
                    }
                });
            }
        });
    }
});
