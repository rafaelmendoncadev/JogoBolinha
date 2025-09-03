// Theme Manager for Ball Sort Puzzle Game
class ThemeManager {
    constructor() {
        this.currentTheme = localStorage.getItem('selectedTheme') || 'classic';
        this.themes = {
            classic: {
                name: 'Clássico',
                description: 'Tema padrão com cores vibrantes'
            },
            dark: {
                name: 'Escuro',
                description: 'Tema escuro para jogos noturnos'
            },
            neon: {
                name: 'Neon',
                description: 'Tema futurista com cores neon'
            },
            nature: {
                name: 'Natureza',
                description: 'Tema inspirado na natureza'
            },
            ocean: {
                name: 'Oceano',
                description: 'Tema inspirado no oceano'
            }
        };
        this.init();
    }

    init() {
        this.applyTheme(this.currentTheme);
        this.createThemeSelector();
    }

    applyTheme(themeName) {
        if (!this.themes[themeName]) {
            console.warn('Theme not found:', themeName);
            return;
        }

        // Remove previous theme
        document.documentElement.removeAttribute('data-theme');
        
        // Apply new theme
        if (themeName !== 'classic') {
            document.documentElement.setAttribute('data-theme', themeName);
        }

        this.currentTheme = themeName;
        localStorage.setItem('selectedTheme', themeName);

        // Update active theme in selector
        this.updateThemeSelector();

        // Trigger custom event for theme change
        const event = new CustomEvent('themeChanged', { 
            detail: { 
                theme: themeName,
                themeData: this.themes[themeName]
            } 
        });
        document.dispatchEvent(event);

        // Update ball colors dynamically
        this.updateBallColors();
    }

    updateBallColors() {
        // Update existing balls with new theme colors
        const balls = document.querySelectorAll('.ball');
        balls.forEach(ball => {
            const color = ball.getAttribute('data-color');
            if (color) {
                // Force style recalculation by temporarily removing and re-adding the attribute
                ball.removeAttribute('data-color');
                setTimeout(() => {
                    ball.setAttribute('data-color', color);
                }, 0);
            }
        });
    }

    createThemeSelector() {
        // Check if theme selector already exists
        if (document.getElementById('theme-selector')) {
            return;
        }

        const themeSelector = document.createElement('div');
        themeSelector.id = 'theme-selector';
        themeSelector.className = 'theme-selector';

        Object.keys(this.themes).forEach(themeKey => {
            const option = document.createElement('div');
            option.className = `theme-option theme-${themeKey}`;
            option.title = `${this.themes[themeKey].name} - ${this.themes[themeKey].description}`;
            option.setAttribute('data-theme', themeKey);
            
            option.addEventListener('click', () => {
                this.applyTheme(themeKey);
                
                // Play theme change sound if available
                if (window.gameEffects && window.gameEffects.soundEnabled) {
                    window.gameEffects.generateTone(600, 0.1, 'triangle', 0.05);
                }
            });

            themeSelector.appendChild(option);
        });

        return themeSelector;
    }

    updateThemeSelector() {
        const options = document.querySelectorAll('.theme-option');
        options.forEach(option => {
            option.classList.remove('active');
            if (option.getAttribute('data-theme') === this.currentTheme) {
                option.classList.add('active');
            }
        });
    }

    getCurrentTheme() {
        return this.currentTheme;
    }

    getThemeInfo(themeName = null) {
        const theme = themeName || this.currentTheme;
        return this.themes[theme];
    }

    getAllThemes() {
        return this.themes;
    }

    // Add theme selector to a specific element
    attachThemeSelector(parentElement) {
        if (typeof parentElement === 'string') {
            parentElement = document.querySelector(parentElement);
        }
        
        if (!parentElement) {
            console.warn('Parent element not found for theme selector');
            return;
        }

        const existingSelector = parentElement.querySelector('#theme-selector');
        if (existingSelector) {
            existingSelector.remove();
        }

        const selector = this.createThemeSelector();
        parentElement.appendChild(selector);
        this.updateThemeSelector();
    }

    // Create theme toggle button
    createThemeToggleButton() {
        const button = document.createElement('button');
        button.className = 'btn btn-outline-secondary btn-effect';
        button.id = 'theme-toggle-btn';
        button.title = 'Alterar tema';
        button.innerHTML = '<i class="fas fa-palette"></i>';

        let dropdownOpen = false;
        const dropdown = document.createElement('div');
        dropdown.className = 'theme-dropdown';
        dropdown.style.cssText = `
            position: absolute;
            top: 100%;
            right: 0;
            background: white;
            border: 1px solid #ddd;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            padding: 10px;
            display: none;
            z-index: 1000;
            min-width: 200px;
        `;

        // Create dropdown content
        const dropdownContent = document.createElement('div');
        dropdownContent.innerHTML = '<div style="font-weight: bold; margin-bottom: 10px;">Escolha um tema:</div>';
        
        const themeGrid = this.createThemeSelector();
        themeGrid.style.margin = '0';
        dropdownContent.appendChild(themeGrid);

        dropdown.appendChild(dropdownContent);

        // Button wrapper for positioning
        const wrapper = document.createElement('div');
        wrapper.style.position = 'relative';
        wrapper.style.display = 'inline-block';
        wrapper.appendChild(button);
        wrapper.appendChild(dropdown);

        button.addEventListener('click', (e) => {
            e.stopPropagation();
            dropdownOpen = !dropdownOpen;
            dropdown.style.display = dropdownOpen ? 'block' : 'none';
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', () => {
            if (dropdownOpen) {
                dropdownOpen = false;
                dropdown.style.display = 'none';
            }
        });

        // Update dropdown theme when theme changes
        document.addEventListener('themeChanged', () => {
            this.updateThemeSelector();
        });

        return wrapper;
    }

    // Get CSS custom property value for current theme
    getThemeVariable(variableName) {
        const style = getComputedStyle(document.documentElement);
        return style.getPropertyValue(variableName);
    }

    // Set theme with animation
    applyThemeWithTransition(themeName, duration = 300) {
        // Add transition class
        document.body.style.transition = `all ${duration}ms ease`;
        
        this.applyTheme(themeName);
        
        // Remove transition after animation
        setTimeout(() => {
            document.body.style.transition = '';
        }, duration);
    }
}

// Initialize theme manager
window.themeManager = new ThemeManager();

// Listen for theme changes and update effects if available
document.addEventListener('themeChanged', (event) => {
    console.log('Theme changed to:', event.detail.theme);
    
    // Update any theme-dependent components
    if (window.gameEffects) {
        // You could add theme-specific sound profiles here
    }
});

// CSS for theme dropdown
const themeCSS = `
.theme-dropdown {
    animation: fadeIn 0.2s ease-out;
}

@keyframes fadeIn {
    from { opacity: 0; transform: translateY(-10px); }
    to { opacity: 1; transform: translateY(0); }
}

[data-theme="dark"] .theme-dropdown {
    background: #34495e;
    color: #ecf0f1;
    border-color: #2c3e50;
}

[data-theme="neon"] .theme-dropdown {
    background: #16213e;
    color: #00ffff;
    border-color: #00ffff;
    box-shadow: 0 4px 12px rgba(0, 255, 255, 0.3);
}

[data-theme="nature"] .theme-dropdown {
    background: #f1f8e9;
    color: #2e7d32;
    border-color: #4a7c59;
}

[data-theme="ocean"] .theme-dropdown {
    background: #e0f7fa;
    color: #00695c;
    border-color: #00695c;
}
`;

// Inject CSS
const themeStyleSheet = document.createElement('style');
themeStyleSheet.type = 'text/css';
themeStyleSheet.innerText = themeCSS;
document.head.appendChild(themeStyleSheet);