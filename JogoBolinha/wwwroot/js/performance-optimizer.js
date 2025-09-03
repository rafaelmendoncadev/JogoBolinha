// Performance Optimizer for Ball Sort Puzzle Game
class PerformanceOptimizer {
    constructor() {
        this.debounceTimers = new Map();
        this.animationFrameCallbacks = new Set();
        this.observers = new Map();
        this.cachedElements = new Map();
        this.performanceMetrics = {
            frameDrops: 0,
            memoryUsage: 0,
            renderTime: 0
        };
        
        this.init();
    }

    init() {
        this.setupPerformanceMonitoring();
        this.optimizeDOM();
        this.setupLazyLoading();
    }

    // Debounce function for event handlers
    debounce(key, func, delay = 300) {
        if (this.debounceTimers.has(key)) {
            clearTimeout(this.debounceTimers.get(key));
        }

        const timer = setTimeout(() => {
            func();
            this.debounceTimers.delete(key);
        }, delay);

        this.debounceTimers.set(key, timer);
    }

    // Throttle function for high-frequency events
    throttle(key, func, limit = 100) {
        if (!this.throttleState) {
            this.throttleState = new Map();
        }

        if (this.throttleState.has(key)) {
            return;
        }

        func();
        this.throttleState.set(key, true);

        setTimeout(() => {
            this.throttleState.delete(key);
        }, limit);
    }

    // Optimized animation frame management
    requestOptimizedFrame(callback) {
        if (this.animationFrameCallbacks.has(callback)) {
            return;
        }

        this.animationFrameCallbacks.add(callback);
        
        requestAnimationFrame(() => {
            this.animationFrameCallbacks.delete(callback);
            callback();
        });
    }

    // DOM caching system
    getCachedElement(selector, forceRefresh = false) {
        if (!forceRefresh && this.cachedElements.has(selector)) {
            return this.cachedElements.get(selector);
        }

        const element = document.querySelector(selector);
        this.cachedElements.set(selector, element);
        return element;
    }

    getCachedElements(selector, forceRefresh = false) {
        const cacheKey = `${selector}_all`;
        if (!forceRefresh && this.cachedElements.has(cacheKey)) {
            return this.cachedElements.get(cacheKey);
        }

        const elements = document.querySelectorAll(selector);
        this.cachedElements.set(cacheKey, elements);
        return elements;
    }

    // Clear cached elements when DOM changes
    clearElementCache(selector = null) {
        if (selector) {
            this.cachedElements.delete(selector);
            this.cachedElements.delete(`${selector}_all`);
        } else {
            this.cachedElements.clear();
        }
    }

    // Performance monitoring
    setupPerformanceMonitoring() {
        // Frame rate monitoring
        let frameCount = 0;
        let lastTime = performance.now();

        const checkFrameRate = () => {
            frameCount++;
            const currentTime = performance.now();
            
            if (currentTime - lastTime >= 1000) {
                const fps = Math.round(frameCount * 1000 / (currentTime - lastTime));
                
                if (fps < 30) {
                    this.performanceMetrics.frameDrops++;
                    this.onPerformanceDrop('low-fps', fps);
                }
                
                frameCount = 0;
                lastTime = currentTime;
            }
            
            requestAnimationFrame(checkFrameRate);
        };

        requestAnimationFrame(checkFrameRate);

        // Memory usage monitoring
        if ('memory' in performance) {
            setInterval(() => {
                const memory = performance.memory;
                this.performanceMetrics.memoryUsage = memory.usedJSHeapSize / memory.jsHeapSizeLimit;
                
                if (this.performanceMetrics.memoryUsage > 0.8) {
                    this.onPerformanceDrop('high-memory', this.performanceMetrics.memoryUsage);
                }
            }, 5000);
        }
    }

    // Handle performance drops
    onPerformanceDrop(type, value) {
        switch (type) {
            case 'low-fps':
                this.optimizeForLowFPS();
                break;
            case 'high-memory':
                this.optimizeMemoryUsage();
                break;
        }
    }

    // FPS optimization
    optimizeForLowFPS() {
        // Reduce animation complexity
        document.documentElement.style.setProperty('--animation-duration', '0.1s');
        
        // Disable non-essential effects
        if (window.gameEffects) {
            const previousSetting = gameEffects.effectsEnabled;
            gameEffects.effectsEnabled = false;
            
            // Re-enable after 5 seconds
            setTimeout(() => {
                gameEffects.effectsEnabled = previousSetting;
                document.documentElement.style.removeProperty('--animation-duration');
            }, 5000);
        }
    }

    // Memory optimization
    optimizeMemoryUsage() {
        // Clear caches
        this.clearElementCache();
        
        // Remove old particle effects
        const particles = document.querySelectorAll('.particle, .confetti, .ball-trail');
        particles.forEach(particle => particle.remove());
        
        // Force garbage collection if available
        if (window.gc && typeof window.gc === 'function') {
            window.gc();
        }
    }

    // DOM optimization
    optimizeDOM() {
        // Use DocumentFragment for batch DOM operations
        this.batchDOMOperations = (operations) => {
            const fragment = document.createDocumentFragment();
            
            operations.forEach(operation => {
                if (operation.type === 'append') {
                    fragment.appendChild(operation.element);
                }
            });
            
            return fragment;
        };

        // Optimize event listeners
        this.addOptimizedEventListener = (element, event, handler, useCapture = false) => {
            const optimizedHandler = this.debounce.bind(this, `${event}_${Date.now()}`, handler, 50);
            element.addEventListener(event, optimizedHandler, useCapture);
            return optimizedHandler;
        };
    }

    // Lazy loading setup
    setupLazyLoading() {
        if ('IntersectionObserver' in window) {
            const lazyObserver = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const element = entry.target;
                        
                        // Load heavy content when visible
                        if (element.dataset.lazyLoad) {
                            this.loadLazyContent(element);
                        }
                        
                        lazyObserver.unobserve(element);
                    }
                });
            }, {
                rootMargin: '50px'
            });

            this.observers.set('lazy', lazyObserver);
        }
    }

    // Load content lazily
    loadLazyContent(element) {
        const contentType = element.dataset.lazyLoad;
        
        switch (contentType) {
            case 'effects':
                this.loadEffectsLazily();
                break;
            case 'sounds':
                this.loadSoundsLazily();
                break;
            case 'animations':
                this.loadAnimationsLazily();
                break;
        }
    }

    // CSS optimization
    optimizeCSS() {
        // Use CSS containment
        const gameArea = document.querySelector('.game-area');
        if (gameArea) {
            gameArea.style.contain = 'layout style paint';
        }

        // Optimize transforms for better performance
        const tubes = document.querySelectorAll('.tube');
        tubes.forEach(tube => {
            tube.style.willChange = 'transform';
            tube.style.transform = 'translateZ(0)'; // Create compositing layer
        });
    }

    // JavaScript optimization
    optimizeJavaScript() {
        // Precompile frequently used selectors
        this.selectors = {
            tubes: '.tube',
            balls: '.ball',
            gameArea: '.game-area',
            selectedTube: '.selected',
            movingBall: '.moving-ball'
        };

        // Object pooling for frequently created objects
        this.objectPools = {
            particleElements: [],
            ballElements: []
        };
    }

    // Object pooling
    getPooledObject(type) {
        const pool = this.objectPools[type];
        return pool && pool.length > 0 ? pool.pop() : null;
    }

    returnPooledObject(type, object) {
        const pool = this.objectPools[type];
        if (pool && object) {
            // Reset object state
            if (object.style) {
                object.style.cssText = '';
            }
            if (object.className) {
                object.className = '';
            }
            
            pool.push(object);
        }
    }

    // Batch update system
    batchUpdate(updates) {
        this.requestOptimizedFrame(() => {
            updates.forEach(update => {
                if (update.element && update.properties) {
                    Object.assign(update.element.style, update.properties);
                }
            });
        });
    }

    // Performance measurement
    measurePerformance(name, func) {
        const start = performance.now();
        const result = func();
        const end = performance.now();
        
        console.log(`${name}: ${(end - start).toFixed(2)}ms`);
        return result;
    }

    // Resource preloading
    preloadResources(resources) {
        resources.forEach(resource => {
            if (resource.type === 'image') {
                const img = new Image();
                img.src = resource.src;
            } else if (resource.type === 'audio') {
                const audio = new Audio();
                audio.preload = 'metadata';
                audio.src = resource.src;
            }
        });
    }

    // Clean up resources
    cleanup() {
        // Clear timers
        this.debounceTimers.forEach(timer => clearTimeout(timer));
        this.debounceTimers.clear();

        // Disconnect observers
        this.observers.forEach(observer => observer.disconnect());
        this.observers.clear();

        // Clear caches
        this.clearElementCache();
        
        // Clear animation callbacks
        this.animationFrameCallbacks.clear();
    }

    // Get performance report
    getPerformanceReport() {
        return {
            metrics: this.performanceMetrics,
            cacheSize: this.cachedElements.size,
            activeObservers: this.observers.size,
            pendingAnimations: this.animationFrameCallbacks.size
        };
    }
}

// Initialize performance optimizer
window.performanceOptimizer = new PerformanceOptimizer();

// Auto-cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (window.performanceOptimizer) {
        window.performanceOptimizer.cleanup();
    }
});