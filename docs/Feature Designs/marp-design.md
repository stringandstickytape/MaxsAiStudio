# Marp Integration Design

All npm/pnpm calls will be handled manually by the user.

## Overview
This document outlines the complete implementation plan for integrating Marp (Markdown Presentation Ecosystem) into the AiStudio application. Marp will be detected through YAML frontmatter in markdown files and render full-featured presentations.

## Implementation Checklist

### 1. Dependencies & Setup
- [x] Install `@marp-team/marp-core` package
- [x] Install `gray-matter` package for frontmatter parsing
- [x] Update package.json with new dependencies
- [x] Add Marp types to TypeScript configuration

**🔍 CHECKPOINT 1: Validate Dependencies**
- Verify packages are installed correctly by running `npm list @marp-team/marp-core gray-matter`
- Check that no TypeScript errors appear after adding types
- Create a simple test file to verify imports work

### 2. Core Components

#### a. MarkdownPane Modifications
- [x] Import gray-matter library
- [x] Add frontmatter parsing logic at the beginning of MarkdownPane component
- [x] Implement conditional rendering based on `marp: true` detection
- [x] Pass content to MarpRenderer when Marp is detected
- [x] Ensure existing markdown rendering continues for non-Marp content

**🔍 CHECKPOINT 2: Validate Frontmatter Detection**
- Test with a markdown file containing `marp: true` in frontmatter
- Test with a regular markdown file (no frontmatter)
- Test with frontmatter but `marp: false` or no marp key
- Verify existing markdown rendering is not broken
- Console.log the detection results to confirm logic works

#### b. MarpRenderer Component (`/components/renderers/marp-renderer.tsx`)
- [x] Create MarpRenderer component with props interface
- [x] Initialize Marp instance with configuration options
- [x] Implement render method to generate HTML/CSS
- [x] Add state management for current slide, presenter mode, etc.
- [x] Implement slide parsing to count total slides
- [x] Handle component lifecycle and cleanup

**🔍 CHECKPOINT 3: Validate Basic Marp Rendering**
- Create a test Marp markdown with 3-4 slides
- Verify HTML and CSS are generated correctly
- Check that slides are displayed (even without navigation)
- Confirm no console errors or warnings
- Verify memory cleanup on component unmount

#### c. MarpControls Component (`/components/renderers/marp-controls.tsx`)
- [x] Create navigation controls UI
- [x] Implement Previous/Next buttons
- [x] Add slide counter display (current/total)
- [x] Create fullscreen toggle button
- [x] Add presenter mode toggle
- [x] Implement export menu (PDF, PPTX, HTML)
- [x] Add thumbnails toggle button

#### d. MarpStyles Component (`/components/renderers/marp-styles.tsx`)
- [x] Create style injection mechanism
- [x] Generate unique scope for each presentation
- [x] Implement CSS-in-JS or scoped style solution
- [x] Handle theme variable integration
- [x] Manage style cleanup on unmount

**🔍 CHECKPOINT 4: Validate Navigation & Styling**
- Test Previous/Next button functionality
- Verify keyboard navigation works (arrow keys)
- Check slide counter updates correctly
- Confirm styles are properly scoped (no global CSS pollution)
- Test with multiple Marp presentations open simultaneously
- Verify theme variables are applied correctly

#### e. MarpThumbnails Component (`/components/renderers/marp-thumbnails.tsx`)
- [x] Create thumbnails sidebar component
- [x] Implement slide preview generation
- [x] Add click-to-navigate functionality
- [x] Highlight current slide
- [x] Add smooth scrolling to current slide
- [x] Implement collapsible sidebar

#### f. MarpPresenterView Component (`/components/renderers/marp-presenter-view.tsx`)
- [x] Create presenter view layout
- [x] Display current slide
- [x] Show next slide preview
- [x] Extract and display speaker notes
- [x] Add timer/stopwatch functionality
- [ ] Implement slide annotations feature

#### g. MarpExport Component (`/components/renderers/marp-export.tsx`)
- [ ] Implement PDF export functionality
- [ ] Add PPTX export (if supported by marp-core)
- [ ] Create HTML bundle export
- [ ] Add share link generation
- [ ] Implement export progress indicator

**🔍 CHECKPOINT 5: Validate Advanced Features**
- Test thumbnail navigation and preview
- Verify presenter view shows notes correctly
- Test export to PDF functionality
- Check that all interactive features work together
- Validate performance with a 20+ slide presentation
- Test on mobile devices for touch gestures

### 3. Navigation & Interaction Features
- [x] Implement keyboard navigation (Arrow keys, Space, Enter, Escape)
- [ ] Add touch gesture support (swipe left/right)
- [ ] Create progress bar component
- [x] Implement "Go to slide" input field
- [ ] Add slide transition animations
- [ ] Handle window resize events for responsive scaling

### 4. Theme Integration
- [ ] Map app theme variables to Marp configuration
- [ ] Support switching between Marp built-in themes (default, gaia, uncover)
- [ ] Integrate with app's dark/light mode
- [ ] Apply custom CSS variables to Marp themes
- [ ] Handle theme persistence

**🔍 CHECKPOINT 6: Validate Theme Integration**
- Switch between all Marp themes and verify rendering
- Toggle app dark/light mode and check Marp adapts
- Verify custom CSS variables are applied
- Test theme persistence across page reloads
- Check for any style conflicts or bleeding

### 5. Performance Optimizations
- [ ] Implement lazy loading for Marp library
- [ ] Add presentation caching mechanism
- [ ] Use React.memo for component optimization
- [ ] Implement debouncing for streaming updates
- [ ] Add virtual rendering for large presentations
- [ ] Optimize thumbnail generation

### 6. Security Implementation
- [ ] Configure Marp HTML sanitization options
- [ ] Set up allowed HTML elements whitelist
- [ ] Ensure CSP compliance for injected styles
- [ ] Sanitize export filenames
- [ ] Validate frontmatter data

### 7. Nested Markdown Support
- [ ] Implement markdown file reference detection
- [ ] Create recursive frontmatter checking
- [ ] Handle embedded Marp presentations
- [ ] Implement file reading mechanism
- [ ] Add error handling for missing files

**🔍 CHECKPOINT 7: Validate Nested Markdown & Security**
- Test markdown files that reference other markdown files
- Verify nested Marp presentations are detected
- Test with missing/invalid file references
- Verify HTML sanitization works correctly
- Test with malicious HTML/scripts in Marp content
- Check CSP compliance with browser console

### 8. CSS & Styling
- [ ] Create base `.marp-container` styles
- [ ] Implement responsive grid layout
- [ ] Style navigation controls
- [ ] Design thumbnail sidebar
- [ ] Style presenter view
- [ ] Add mobile-responsive styles
- [ ] Create print styles for PDF export

### 9. Error Handling
- [ ] Add try-catch blocks for Marp rendering
- [ ] Implement fallback UI for render failures
- [ ] Add error boundaries for React components
- [ ] Create user-friendly error messages
- [ ] Log errors appropriately

**🔍 CHECKPOINT 8: Validate Error Handling & Edge Cases**
- Test with invalid Marp syntax
- Test with extremely large presentations (100+ slides)
- Test rapid switching between presentations
- Verify error messages are helpful
- Check fallback UI displays correctly
- Test recovery from errors

### 10. Final Integration Validation
- [ ] Test basic Marp detection and rendering
- [ ] Verify all navigation methods work
- [ ] Test export functionality
- [ ] Verify presenter mode features
- [ ] Test nested markdown handling
- [ ] Verify theme switching
- [ ] Test responsive behavior
- [ ] Validate accessibility features

**🔍 FINAL CHECKPOINT: Complete System Validation**
- Run through a complete user workflow:
  1. Create a Marp presentation with 10+ slides
  2. Include speaker notes, images, and code blocks
  3. Test all navigation methods
  4. Switch themes
  5. Enter presenter mode
  6. Export to PDF
  7. Test on different screen sizes
- Verify no regressions in existing markdown functionality
- Check performance metrics
- Validate all features work together harmoniously

## Code Structure

```
/components/
  /renderers/
    marp-renderer.tsx          # Main Marp renderer component
    marp-controls.tsx          # Navigation controls
    marp-styles.tsx            # Style injection logic
    marp-presenter-view.tsx    # Presenter view component
    marp-export.tsx            # Export functionality
    marp-thumbnails.tsx        # Slide thumbnails sidebar
  MarkdownPane.tsx             # Modified to detect Marp content
```

## Example Marp Markdown

```markdown
---
marp: true
theme: gaia
size: 16:9
paginate: true
---

# Welcome to Marp

Your presentation starts here

---

# Slide 2

With speaker notes

<!-- 
Speaker notes go here and won't be visible in the presentation
-->

---

# Features

- Bullet points
- Math support: $f(x) = x^2$
- Code blocks
- Images and diagrams
```

## Configuration Options

The Marp instance should be initialized with these options:
```typescript
const marp = new Marp({
  html: true,
  emoji: { shortcode: 'twemoji', unicode: 'twemoji' },
  math: 'mathjax',
  script: { source: 'cdn' },
  minifyCSS: true,
  allowLocalFiles: true
});
```

## Completion Criteria

- [ ] All components implemented and integrated
- [ ] Marp presentations render correctly
- [ ] All navigation methods functional
- [ ] Export features working
- [ ] Presenter mode operational
- [ ] Responsive design implemented
- [ ] Performance optimizations in place
- [ ] Security measures implemented
- [ ] Error handling comprehensive
- [ ] Documentation updated