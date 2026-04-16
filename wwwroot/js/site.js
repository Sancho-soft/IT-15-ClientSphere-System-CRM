// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

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
    }
});
