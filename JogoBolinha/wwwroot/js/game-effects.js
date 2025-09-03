// Game Effects System - Visual and Audio Effects for Ball Sort Puzzle
class GameEffects {
    constructor() {
        this.soundEnabled = localStorage.getItem('soundEnabled') !== 'false';
        this.effectsEnabled = localStorage.getItem('effectsEnabled') !== 'false';
        this.audioContext = null;
        this.sounds = {};
        this.particleContainer = null;
        this.initializeAudioContext();
        this.createParticleContainer();
    }

    initializeAudioContext() {
        try {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
        } catch (error) {
            console.warn('Audio not supported:', error);
        }
    }

    createParticleContainer() {
        this.particleContainer = document.createElement('div');
        this.particleContainer.id = 'particle-container';
        this.particleContainer.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            z-index: 9998;
        `;
        document.body.appendChild(this.particleContainer);
    }

    // Sound generation using Web Audio API
    generateTone(frequency, duration, type = 'sine', volume = 0.1) {
        if (!this.soundEnabled || !this.audioContext) return;

        const oscillator = this.audioContext.createOscillator();
        const gainNode = this.audioContext.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(this.audioContext.destination);

        oscillator.frequency.setValueAtTime(frequency, this.audioContext.currentTime);
        oscillator.type = type;

        gainNode.gain.setValueAtTime(0, this.audioContext.currentTime);
        gainNode.gain.linearRampToValueAtTime(volume, this.audioContext.currentTime + 0.01);
        gainNode.gain.exponentialRampToValueAtTime(0.001, this.audioContext.currentTime + duration);

        oscillator.start(this.audioContext.currentTime);
        oscillator.stop(this.audioContext.currentTime + duration);
    }

    // Play move sound with color-based pitch
    playMoveSound(ballColor) {
        const colorFrequencies = {
            '#ff6b6b': 261.63, // Red - C4
            '#4ecdc4': 293.66, // Cyan - D4
            '#45b7d1': 329.63, // Blue - E4
            '#96ceb4': 349.23, // Green - F4
            '#ffeaa7': 392.00, // Yellow - G4
            '#fab1a0': 440.00, // Orange - A4
            '#6c5ce7': 493.88, // Purple - B4
            '#fd79a8': 523.25, // Pink - C5
            '#00b894': 587.33, // Teal - D5
            '#e17055': 659.25, // Brown - E5
        };

        const frequency = colorFrequencies[ballColor] || 440;
        this.generateTone(frequency, 0.15, 'triangle', 0.08);
    }

    // Play victory fanfare
    playVictorySound() {
        if (!this.soundEnabled) return;
        
        const notes = [523.25, 659.25, 783.99, 1046.50]; // C5, E5, G5, C6
        notes.forEach((note, index) => {
            setTimeout(() => {
                this.generateTone(note, 0.3, 'triangle', 0.12);
            }, index * 100);
        });
    }

    // Play error sound
    playErrorSound() {
        if (!this.soundEnabled) return;
        this.generateTone(200, 0.3, 'sawtooth', 0.05);
    }

    // Play hint sound
    playHintSound() {
        if (!this.soundEnabled) return;
        this.generateTone(800, 0.2, 'sine', 0.06);
        setTimeout(() => {
            this.generateTone(1000, 0.2, 'sine', 0.06);
        }, 200);
    }

    // Play undo sound
    playUndoSound() {
        if (!this.soundEnabled) return;
        this.generateTone(600, 0.15, 'triangle', 0.05);
        setTimeout(() => {
            this.generateTone(400, 0.15, 'triangle', 0.05);
        }, 100);
    }

    // Create particle explosion effect
    createParticleExplosion(x, y, color, count = 20) {
        if (!this.effectsEnabled) return;

        for (let i = 0; i < count; i++) {
            const particle = document.createElement('div');
            particle.className = 'particle';
            
            const size = Math.random() * 8 + 4;
            const angle = (Math.PI * 2 * i) / count + (Math.random() * 0.5 - 0.25);
            const velocity = Math.random() * 100 + 50;
            const life = Math.random() * 1000 + 500;
            
            particle.style.cssText = `
                position: absolute;
                left: ${x}px;
                top: ${y}px;
                width: ${size}px;
                height: ${size}px;
                background-color: ${color};
                border-radius: 50%;
                pointer-events: none;
                animation: particleFloat ${life}ms ease-out forwards;
                --dx: ${Math.cos(angle) * velocity}px;
                --dy: ${Math.sin(angle) * velocity}px;
            `;

            this.particleContainer.appendChild(particle);

            setTimeout(() => {
                if (particle.parentNode) {
                    particle.parentNode.removeChild(particle);
                }
            }, life);
        }
    }

    // Create victory confetti effect
    createVictoryConfetti() {
        if (!this.effectsEnabled) return;

        const colors = ['#ff6b6b', '#4ecdc4', '#45b7d1', '#96ceb4', '#ffeaa7', '#fab1a0'];
        const confettiCount = 50;

        for (let i = 0; i < confettiCount; i++) {
            setTimeout(() => {
                const confetti = document.createElement('div');
                confetti.className = 'confetti';
                
                const color = colors[Math.floor(Math.random() * colors.length)];
                const x = Math.random() * window.innerWidth;
                const rotation = Math.random() * 360;
                const scale = Math.random() * 0.8 + 0.4;
                const duration = Math.random() * 2000 + 3000;
                
                confetti.style.cssText = `
                    position: absolute;
                    left: ${x}px;
                    top: -20px;
                    width: 10px;
                    height: 10px;
                    background-color: ${color};
                    transform: rotate(${rotation}deg) scale(${scale});
                    animation: confettiFall ${duration}ms linear forwards;
                `;

                this.particleContainer.appendChild(confetti);

                setTimeout(() => {
                    if (confetti.parentNode) {
                        confetti.parentNode.removeChild(confetti);
                    }
                }, duration);
            }, Math.random() * 500);
        }
    }

    // Simplified ball movement animation
    animateBallMove(fromElement, toElement, ballElement, ballColor, onComplete) {
        // This function is disabled to prevent hanging - use fallback in main game code
        onComplete();
        return;
    }

    // Create ball trail effect
    createBallTrail(ballElement, color) {
        let trailCount = 0;
        const maxTrails = 5;
        
        const trailInterval = setInterval(() => {
            if (trailCount >= maxTrails) {
                clearInterval(trailInterval);
                return;
            }
            
            const trail = ballElement.clone();
            trail.addClass('ball-trail');
            trail.css({
                position: 'absolute',
                left: ballElement.css('left'),
                top: ballElement.css('top'),
                opacity: 0.6 - (trailCount * 0.1),
                transform: `scale(${1.0 - (trailCount * 0.1)})`,
                zIndex: 1000 - trailCount,
                pointerEvents: 'none'
            });
            
            $('body').append(trail);
            
            trail.animate({ opacity: 0 }, 300, function() {
                trail.remove();
            });
            
            trailCount++;
        }, 50);
        
        setTimeout(() => clearInterval(trailInterval), 600);
    }

    // Tube completion celebration
    celebrateTubeCompletion(tubeElement, color) {
        if (!this.effectsEnabled) return;

        const tubePos = tubeElement.offset();
        const centerX = tubePos.left + tubeElement.width() / 2;
        const centerY = tubePos.top + tubeElement.height() / 2;

        // Create sparkle effect
        this.createParticleExplosion(centerX, centerY, color, 15);
        
        // Add glowing border temporarily
        tubeElement.css({
            boxShadow: `0 0 30px ${color}`,
            transition: 'box-shadow 0.5s ease'
        });
        
        setTimeout(() => {
            tubeElement.css('box-shadow', '');
        }, 2000);

        // Play completion sound
        this.generateTone(523.25, 0.4, 'triangle', 0.1);
    }

    // Screen shake effect
    shakeScreen(intensity = 10, duration = 300) {
        if (!this.effectsEnabled) return;

        const body = $('body');
        const originalTransform = body.css('transform');
        
        const shake = () => {
            const x = (Math.random() - 0.5) * intensity;
            const y = (Math.random() - 0.5) * intensity;
            body.css('transform', `translate(${x}px, ${y}px)`);
        };
        
        const shakeInterval = setInterval(shake, 50);
        
        setTimeout(() => {
            clearInterval(shakeInterval);
            body.css('transform', originalTransform);
        }, duration);
    }

    // Toggle sound
    toggleSound() {
        this.soundEnabled = !this.soundEnabled;
        localStorage.setItem('soundEnabled', this.soundEnabled);
        
        if (this.soundEnabled) {
            this.playHintSound();
        }
        
        return this.soundEnabled;
    }

    // Toggle effects
    toggleEffects() {
        this.effectsEnabled = !this.effectsEnabled;
        localStorage.setItem('effectsEnabled', this.effectsEnabled);
        return this.effectsEnabled;
    }
}

// CSS animations for particles and effects
const effectsCSS = `
@keyframes particleFloat {
    0% {
        opacity: 1;
        transform: translate(0, 0) scale(1);
    }
    100% {
        opacity: 0;
        transform: translate(var(--dx), var(--dy)) scale(0.3);
    }
}

@keyframes confettiFall {
    0% {
        transform: translateY(0) rotate(0deg);
        opacity: 1;
    }
    100% {
        transform: translateY(100vh) rotate(720deg);
        opacity: 0;
    }
}

@keyframes ballBounce {
    0%, 100% { transform: translateY(0) scale(1); }
    50% { transform: translateY(-10px) scale(1.1); }
}

@keyframes tubeGlow {
    0%, 100% { box-shadow: 0 0 10px rgba(0,0,0,0.1); }
    50% { box-shadow: 0 0 25px var(--glow-color); }
}

.moving-ball-enhanced {
    transition: all 0.6s cubic-bezier(0.25, 0.46, 0.45, 0.94);
    filter: drop-shadow(0 5px 10px rgba(0,0,0,0.3));
}

.ball-trail {
    transition: opacity 0.3s ease-out;
}

.tube-completed {
    animation: tubeGlow 1s ease-in-out 3;
}

.ball-hover-effect:hover {
    animation: ballBounce 0.5s ease-in-out;
}

/* Enhanced button effects */
.btn-effect {
    position: relative;
    overflow: hidden;
}

.btn-effect::before {
    content: '';
    position: absolute;
    top: 0;
    left: -100%;
    width: 100%;
    height: 100%;
    background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
    transition: left 0.5s;
}

.btn-effect:hover::before {
    left: 100%;
}
`;

// Inject CSS
const styleSheet = document.createElement('style');
styleSheet.type = 'text/css';
styleSheet.innerText = effectsCSS;
document.head.appendChild(styleSheet);

// Global instance
window.gameEffects = new GameEffects();