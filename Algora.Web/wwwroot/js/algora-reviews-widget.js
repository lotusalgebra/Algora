/**
 * Algora Reviews Widget
 * Embeddable JavaScript widget for displaying product reviews on Shopify stores
 *
 * Usage:
 * <script src="https://yourapp.com/js/algora-reviews-widget.js"
 *         data-api-key="YOUR_API_KEY"
 *         data-product-id="{{product.id}}" async></script>
 * <div id="algora-reviews"></div>
 */

(function() {
    'use strict';

    // Configuration
    const WIDGET_VERSION = '1.0.0';
    const DEFAULT_REVIEWS_PER_PAGE = 10;

    // Get script element and configuration
    const currentScript = document.currentScript || (function() {
        const scripts = document.getElementsByTagName('script');
        return scripts[scripts.length - 1];
    })();

    const config = {
        apiKey: currentScript.getAttribute('data-api-key'),
        productId: currentScript.getAttribute('data-product-id'),
        containerId: currentScript.getAttribute('data-container') || 'algora-reviews',
        apiBaseUrl: currentScript.getAttribute('data-api-url') || getBaseUrl(),
        theme: currentScript.getAttribute('data-theme') || 'light',
        reviewsPerPage: parseInt(currentScript.getAttribute('data-per-page')) || DEFAULT_REVIEWS_PER_PAGE,
        showWriteReview: currentScript.getAttribute('data-show-write-review') !== 'false',
        showPhotos: currentScript.getAttribute('data-show-photos') !== 'false',
        locale: currentScript.getAttribute('data-locale') || 'en'
    };

    function getBaseUrl() {
        const src = currentScript.src;
        const url = new URL(src);
        return url.origin;
    }

    // State
    let state = {
        reviews: [],
        summary: null,
        currentPage: 1,
        totalPages: 1,
        loading: false,
        settings: null,
        showWriteForm: false
    };

    // Styles injection
    function injectStyles() {
        if (document.getElementById('algora-reviews-styles')) return;

        const link = document.createElement('link');
        link.id = 'algora-reviews-styles';
        link.rel = 'stylesheet';
        link.href = config.apiBaseUrl + '/css/algora-reviews-widget.css';
        document.head.appendChild(link);
    }

    // API calls
    async function fetchSummary() {
        try {
            const response = await fetch(
                `${config.apiBaseUrl}/api/reviews/widget/${config.apiKey}/summary/${config.productId}`
            );
            if (!response.ok) throw new Error('Failed to fetch summary');
            return await response.json();
        } catch (error) {
            console.error('Algora Reviews: Error fetching summary', error);
            return null;
        }
    }

    async function fetchReviews(page = 1) {
        try {
            const params = new URLSearchParams({
                page: page.toString(),
                pageSize: config.reviewsPerPage.toString()
            });

            const response = await fetch(
                `${config.apiBaseUrl}/api/reviews/widget/${config.apiKey}/product/${config.productId}?${params}`
            );
            if (!response.ok) throw new Error('Failed to fetch reviews');
            return await response.json();
        } catch (error) {
            console.error('Algora Reviews: Error fetching reviews', error);
            return { items: [], totalCount: 0, page: 1, pageSize: config.reviewsPerPage, totalPages: 0 };
        }
    }

    async function submitReview(reviewData) {
        try {
            const response = await fetch(`${config.apiBaseUrl}/api/reviews/submit`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    ...reviewData,
                    apiKey: config.apiKey,
                    platformProductId: parseInt(config.productId)
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to submit review');
            }

            return await response.json();
        } catch (error) {
            console.error('Algora Reviews: Error submitting review', error);
            throw error;
        }
    }

    // Utility functions
    function formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString(config.locale, {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function generateStars(rating, interactive = false, size = 'md') {
        const stars = [];
        for (let i = 1; i <= 5; i++) {
            const filled = i <= rating;
            const starClass = interactive ? 'algora-star-interactive' : '';
            stars.push(`
                <span class="algora-star algora-star-${size} ${starClass} ${filled ? 'algora-star-filled' : 'algora-star-empty'}"
                      data-rating="${i}">
                    ${filled ? '★' : '☆'}
                </span>
            `);
        }
        return `<span class="algora-stars">${stars.join('')}</span>`;
    }

    // Render functions
    function renderSummary() {
        if (!state.summary) return '';

        const { averageRating, totalReviews, ratingDistribution } = state.summary;
        const maxCount = Math.max(...Object.values(ratingDistribution), 1);

        const distributionBars = [5, 4, 3, 2, 1].map(rating => {
            const count = ratingDistribution[rating] || 0;
            const percentage = totalReviews > 0 ? (count / totalReviews * 100) : 0;
            return `
                <div class="algora-rating-bar" data-filter-rating="${rating}">
                    <span class="algora-rating-label">${rating} stars</span>
                    <div class="algora-bar-container">
                        <div class="algora-bar-fill" style="width: ${percentage}%"></div>
                    </div>
                    <span class="algora-rating-count">${count}</span>
                </div>
            `;
        }).join('');

        return `
            <div class="algora-summary">
                <div class="algora-summary-left">
                    <div class="algora-average-rating">${averageRating.toFixed(1)}</div>
                    ${generateStars(Math.round(averageRating), false, 'lg')}
                    <div class="algora-total-reviews">${totalReviews} review${totalReviews !== 1 ? 's' : ''}</div>
                </div>
                <div class="algora-summary-right">
                    ${distributionBars}
                </div>
            </div>
        `;
    }

    function renderWriteReviewButton() {
        if (!config.showWriteReview) return '';

        return `
            <button class="algora-btn algora-btn-primary algora-write-review-btn" id="algora-write-review-btn">
                Write a Review
            </button>
        `;
    }

    function renderWriteReviewForm() {
        if (!state.showWriteForm) return '';

        return `
            <div class="algora-write-form" id="algora-write-form">
                <h3 class="algora-form-title">Write a Review</h3>
                <form id="algora-review-form">
                    <div class="algora-form-group">
                        <label class="algora-label">Rating *</label>
                        <div class="algora-star-rating" id="algora-star-rating">
                            ${generateStars(0, true, 'lg')}
                        </div>
                        <input type="hidden" name="rating" id="algora-rating-input" value="0" required>
                    </div>

                    <div class="algora-form-row">
                        <div class="algora-form-group">
                            <label class="algora-label" for="algora-reviewer-name">Your Name *</label>
                            <input type="text" id="algora-reviewer-name" name="reviewerName"
                                   class="algora-input" required placeholder="John Doe">
                        </div>
                        <div class="algora-form-group">
                            <label class="algora-label" for="algora-reviewer-email">Email (optional)</label>
                            <input type="email" id="algora-reviewer-email" name="reviewerEmail"
                                   class="algora-input" placeholder="john@example.com">
                        </div>
                    </div>

                    <div class="algora-form-group">
                        <label class="algora-label" for="algora-review-title">Review Title</label>
                        <input type="text" id="algora-review-title" name="title"
                               class="algora-input" placeholder="Summarize your review">
                    </div>

                    <div class="algora-form-group">
                        <label class="algora-label" for="algora-review-body">Your Review *</label>
                        <textarea id="algora-review-body" name="body" class="algora-textarea"
                                  rows="5" required placeholder="Share your experience with this product..."></textarea>
                    </div>

                    <div class="algora-form-actions">
                        <button type="button" class="algora-btn algora-btn-secondary" id="algora-cancel-review">
                            Cancel
                        </button>
                        <button type="submit" class="algora-btn algora-btn-primary" id="algora-submit-review">
                            Submit Review
                        </button>
                    </div>

                    <div id="algora-form-message" class="algora-form-message"></div>
                </form>
            </div>
        `;
    }

    function renderReviewMedia(media) {
        if (!media || media.length === 0 || !config.showPhotos) return '';

        const images = media.filter(m => m.mediaType === 'image').slice(0, 4);
        if (images.length === 0) return '';

        return `
            <div class="algora-review-media">
                ${images.map(img => `
                    <a href="${escapeHtml(img.url)}" target="_blank" class="algora-media-thumb">
                        <img src="${escapeHtml(img.thumbnailUrl || img.url)}"
                             alt="${escapeHtml(img.altText || 'Review photo')}"
                             loading="lazy">
                    </a>
                `).join('')}
            </div>
        `;
    }

    function renderReview(review) {
        return `
            <div class="algora-review" data-review-id="${review.id}">
                <div class="algora-review-header">
                    <div class="algora-review-meta">
                        ${generateStars(review.rating)}
                        ${review.isVerifiedPurchase ? '<span class="algora-verified-badge">Verified Purchase</span>' : ''}
                    </div>
                    <div class="algora-review-date">${formatDate(review.reviewDate)}</div>
                </div>

                ${review.title ? `<h4 class="algora-review-title">${escapeHtml(review.title)}</h4>` : ''}

                <div class="algora-review-body">
                    ${escapeHtml(review.body)}
                </div>

                ${renderReviewMedia(review.media)}

                <div class="algora-review-footer">
                    <span class="algora-reviewer-name">${escapeHtml(review.reviewerName)}</span>
                    ${review.productTitle ? `<span class="algora-product-name">on ${escapeHtml(review.productTitle)}</span>` : ''}
                </div>

                <div class="algora-review-helpful">
                    <span>Was this review helpful?</span>
                    <button class="algora-helpful-btn algora-helpful-yes" data-vote="helpful" data-review-id="${review.id}">
                        Yes (${review.helpfulVotes || 0})
                    </button>
                    <button class="algora-helpful-btn algora-helpful-no" data-vote="unhelpful" data-review-id="${review.id}">
                        No (${review.unhelpfulVotes || 0})
                    </button>
                </div>
            </div>
        `;
    }

    function renderReviewsList() {
        if (state.loading) {
            return `
                <div class="algora-loading">
                    <div class="algora-spinner"></div>
                    <p>Loading reviews...</p>
                </div>
            `;
        }

        if (state.reviews.length === 0) {
            return `
                <div class="algora-no-reviews">
                    <p>No reviews yet. Be the first to review this product!</p>
                </div>
            `;
        }

        return `
            <div class="algora-reviews-list">
                ${state.reviews.map(renderReview).join('')}
            </div>
        `;
    }

    function renderPagination() {
        if (state.totalPages <= 1) return '';

        const pages = [];
        const maxVisible = 5;
        let start = Math.max(1, state.currentPage - Math.floor(maxVisible / 2));
        let end = Math.min(state.totalPages, start + maxVisible - 1);

        if (end - start < maxVisible - 1) {
            start = Math.max(1, end - maxVisible + 1);
        }

        if (state.currentPage > 1) {
            pages.push(`<button class="algora-page-btn" data-page="${state.currentPage - 1}">← Prev</button>`);
        }

        if (start > 1) {
            pages.push(`<button class="algora-page-btn" data-page="1">1</button>`);
            if (start > 2) {
                pages.push(`<span class="algora-page-ellipsis">...</span>`);
            }
        }

        for (let i = start; i <= end; i++) {
            const active = i === state.currentPage ? 'algora-page-active' : '';
            pages.push(`<button class="algora-page-btn ${active}" data-page="${i}">${i}</button>`);
        }

        if (end < state.totalPages) {
            if (end < state.totalPages - 1) {
                pages.push(`<span class="algora-page-ellipsis">...</span>`);
            }
            pages.push(`<button class="algora-page-btn" data-page="${state.totalPages}">${state.totalPages}</button>`);
        }

        if (state.currentPage < state.totalPages) {
            pages.push(`<button class="algora-page-btn" data-page="${state.currentPage + 1}">Next →</button>`);
        }

        return `
            <div class="algora-pagination">
                ${pages.join('')}
            </div>
        `;
    }

    function render() {
        const container = document.getElementById(config.containerId);
        if (!container) {
            console.error(`Algora Reviews: Container #${config.containerId} not found`);
            return;
        }

        container.innerHTML = `
            <div class="algora-widget algora-theme-${config.theme}">
                <div class="algora-header">
                    <h2 class="algora-title">Customer Reviews</h2>
                    ${renderWriteReviewButton()}
                </div>

                ${renderSummary()}
                ${renderWriteReviewForm()}
                ${renderReviewsList()}
                ${renderPagination()}

                <div class="algora-footer">
                    <a href="https://algora.app" target="_blank" class="algora-powered-by">
                        Powered by Algora
                    </a>
                </div>
            </div>
        `;

        attachEventListeners();
    }

    // Event handling
    function attachEventListeners() {
        const container = document.getElementById(config.containerId);
        if (!container) return;

        // Write review button
        const writeBtn = container.querySelector('#algora-write-review-btn');
        if (writeBtn) {
            writeBtn.addEventListener('click', () => {
                state.showWriteForm = true;
                render();
                document.getElementById('algora-write-form')?.scrollIntoView({ behavior: 'smooth' });
            });
        }

        // Cancel review button
        const cancelBtn = container.querySelector('#algora-cancel-review');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => {
                state.showWriteForm = false;
                render();
            });
        }

        // Star rating interaction
        const starRating = container.querySelector('#algora-star-rating');
        if (starRating) {
            starRating.querySelectorAll('.algora-star-interactive').forEach(star => {
                star.addEventListener('click', (e) => {
                    const rating = parseInt(e.target.dataset.rating);
                    document.getElementById('algora-rating-input').value = rating;
                    updateStarDisplay(rating);
                });

                star.addEventListener('mouseenter', (e) => {
                    const rating = parseInt(e.target.dataset.rating);
                    updateStarDisplay(rating, true);
                });
            });

            starRating.addEventListener('mouseleave', () => {
                const currentRating = parseInt(document.getElementById('algora-rating-input').value);
                updateStarDisplay(currentRating);
            });
        }

        // Review form submission
        const form = container.querySelector('#algora-review-form');
        if (form) {
            form.addEventListener('submit', handleFormSubmit);
        }

        // Pagination
        container.querySelectorAll('.algora-page-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const page = parseInt(e.target.dataset.page);
                if (page && page !== state.currentPage) {
                    await loadReviews(page);
                }
            });
        });

        // Rating bar filter
        container.querySelectorAll('.algora-rating-bar').forEach(bar => {
            bar.addEventListener('click', (e) => {
                const rating = e.currentTarget.dataset.filterRating;
                // Future: implement filtering by rating
                console.log('Filter by rating:', rating);
            });
        });

        // Helpful votes
        container.querySelectorAll('.algora-helpful-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const reviewId = e.target.dataset.reviewId;
                const vote = e.target.dataset.vote;
                // Future: implement helpful voting
                console.log('Vote:', vote, 'for review:', reviewId);
            });
        });
    }

    function updateStarDisplay(rating, hover = false) {
        const starRating = document.getElementById('algora-star-rating');
        if (!starRating) return;

        starRating.querySelectorAll('.algora-star-interactive').forEach(star => {
            const starRating = parseInt(star.dataset.rating);
            if (starRating <= rating) {
                star.classList.add('algora-star-filled');
                star.classList.remove('algora-star-empty');
                star.textContent = '★';
            } else {
                star.classList.remove('algora-star-filled');
                star.classList.add('algora-star-empty');
                star.textContent = '☆';
            }
        });
    }

    async function handleFormSubmit(e) {
        e.preventDefault();

        const form = e.target;
        const submitBtn = form.querySelector('#algora-submit-review');
        const messageDiv = form.querySelector('#algora-form-message');

        const rating = parseInt(form.querySelector('[name="rating"]').value);
        if (rating < 1 || rating > 5) {
            showFormMessage(messageDiv, 'Please select a rating', 'error');
            return;
        }

        const reviewData = {
            rating: rating,
            reviewerName: form.querySelector('[name="reviewerName"]').value.trim(),
            reviewerEmail: form.querySelector('[name="reviewerEmail"]').value.trim() || null,
            title: form.querySelector('[name="title"]').value.trim() || null,
            body: form.querySelector('[name="body"]').value.trim()
        };

        if (!reviewData.reviewerName) {
            showFormMessage(messageDiv, 'Please enter your name', 'error');
            return;
        }

        if (!reviewData.body) {
            showFormMessage(messageDiv, 'Please write a review', 'error');
            return;
        }

        submitBtn.disabled = true;
        submitBtn.textContent = 'Submitting...';

        try {
            await submitReview(reviewData);
            showFormMessage(messageDiv, 'Thank you! Your review has been submitted and is pending approval.', 'success');
            form.reset();
            document.getElementById('algora-rating-input').value = '0';
            updateStarDisplay(0);

            setTimeout(() => {
                state.showWriteForm = false;
                render();
            }, 3000);
        } catch (error) {
            showFormMessage(messageDiv, error.message || 'Failed to submit review. Please try again.', 'error');
        } finally {
            submitBtn.disabled = false;
            submitBtn.textContent = 'Submit Review';
        }
    }

    function showFormMessage(element, message, type) {
        if (!element) return;
        element.textContent = message;
        element.className = `algora-form-message algora-message-${type}`;
        element.style.display = 'block';
    }

    // Data loading
    async function loadReviews(page = 1) {
        state.loading = true;
        state.currentPage = page;
        render();

        const result = await fetchReviews(page);
        state.reviews = result.items || [];
        state.totalPages = result.totalPages || 1;
        state.loading = false;

        render();

        // Scroll to top of widget
        document.getElementById(config.containerId)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    // Initialization
    async function init() {
        if (!config.apiKey) {
            console.error('Algora Reviews: API key is required');
            return;
        }

        if (!config.productId) {
            console.error('Algora Reviews: Product ID is required');
            return;
        }

        injectStyles();

        // Show loading state
        state.loading = true;
        render();

        // Load summary and reviews in parallel
        const [summary, reviewsResult] = await Promise.all([
            fetchSummary(),
            fetchReviews(1)
        ]);

        state.summary = summary;
        state.reviews = reviewsResult.items || [];
        state.totalPages = reviewsResult.totalPages || 1;
        state.loading = false;

        render();
    }

    // Start when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose API for external control
    window.AlgoraReviews = {
        refresh: () => loadReviews(state.currentPage),
        loadPage: loadReviews,
        getState: () => ({ ...state }),
        version: WIDGET_VERSION
    };
})();
