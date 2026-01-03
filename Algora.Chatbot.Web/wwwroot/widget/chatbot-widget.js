(function(window, document) {
  'use strict';

  // Configuration
  var API_BASE = '';
  var config = {};
  var sessionId = '';
  var visitorId = '';
  var conversationId = null;
  var isOpen = false;
  var container = null;
  var isEscalated = false;
  var pollInterval = null;
  var lastMessageTime = null;
  var signalRConnection = null;
  var useSignalR = false;

  // Generate unique IDs
  function generateId() {
    return 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  // Get or create session/visitor IDs
  function getSessionId() {
    var id = sessionStorage.getItem('algora_session_id');
    if (!id) {
      id = generateId();
      sessionStorage.setItem('algora_session_id', id);
    }
    return id;
  }

  function getVisitorId() {
    var id = localStorage.getItem('algora_visitor_id');
    if (!id) {
      id = generateId();
      localStorage.setItem('algora_visitor_id', id);
    }
    return id;
  }

  // API calls
  async function fetchConfig(shop) {
    try {
      var response = await fetch(API_BASE + '/api/widget/v1/config/' + encodeURIComponent(shop));
      var data = await response.json();
      if (data.success) {
        return data.config;
      }
    } catch (e) {
      console.error('Failed to fetch widget config:', e);
    }
    return getDefaultConfig();
  }

  function getDefaultConfig() {
    return {
      botName: 'Support Assistant',
      welcomeMessage: 'Hi! How can I help you today?',
      position: 'bottom-right',
      primaryColor: '#7c3aed',
      headerTitle: 'Chat with us',
      triggerText: 'Need help?',
      showPoweredBy: true,
      placeholderText: 'Type your message...'
    };
  }

  async function startConversation(shop) {
    try {
      var response = await fetch(API_BASE + '/api/widget/v1/conversations/start', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          shop: shop,
          sessionId: sessionId,
          visitorId: visitorId,
          pageUrl: window.location.href
        })
      });
      var data = await response.json();
      if (data.success) {
        conversationId = data.conversationId;
        return data;
      }
    } catch (e) {
      console.error('Failed to start conversation:', e);
    }
    return null;
  }

  async function sendMessage(shop, message) {
    if (!conversationId) {
      var conv = await startConversation(shop);
      if (!conv) return null;
    }

    try {
      var response = await fetch(API_BASE + '/api/widget/v1/conversations/' + conversationId + '/messages', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          shop: shop,
          sessionId: sessionId,
          message: message
        })
      });
      return await response.json();
    } catch (e) {
      console.error('Failed to send message:', e);
      return null;
    }
  }

  async function escalateToHuman(reason) {
    if (!conversationId) return null;

    try {
      var response = await fetch(API_BASE + '/api/widget/v1/conversations/' + conversationId + '/escalate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason: reason })
      });
      var data = await response.json();
      if (data.success) {
        isEscalated = true;
        updateEscalatedUI();
        // SignalR/polling is handled in handleEscalate()
      }
      return data;
    } catch (e) {
      console.error('Failed to escalate:', e);
      return null;
    }
  }

  async function pollForMessages() {
    if (!conversationId || !isEscalated) return;

    try {
      var url = API_BASE + '/api/widget/v1/conversations/' + conversationId + '/poll';
      if (lastMessageTime) {
        url += '?since=' + encodeURIComponent(lastMessageTime);
      }

      var response = await fetch(url);
      var data = await response.json();

      if (data.success && data.messages && data.messages.length > 0) {
        data.messages.forEach(function(msg) {
          if (!document.querySelector('[data-msg-id="' + msg.id + '"]')) {
            addMessage(msg.content, msg.role, msg.id);
            lastMessageTime = msg.createdAt;
          }
        });
      }

      // Check if conversation is resolved
      if (data.status === 'resolved') {
        stopPolling();
        isEscalated = false;
        updateResolvedUI();
      }
    } catch (e) {
      console.error('Failed to poll messages:', e);
    }
  }

  function startPolling() {
    if (pollInterval) return;
    pollInterval = setInterval(pollForMessages, 3000); // Poll every 3 seconds
  }

  function stopPolling() {
    if (pollInterval) {
      clearInterval(pollInterval);
      pollInterval = null;
    }
  }

  // SignalR connection for real-time messaging
  async function connectSignalR() {
    if (signalRConnection || !conversationId) return;

    // Check if SignalR is available
    if (typeof signalR === 'undefined') {
      // Try to load SignalR dynamically
      try {
        await loadSignalRScript();
      } catch (e) {
        console.log('SignalR not available, using polling fallback');
        return;
      }
    }

    try {
      signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl(API_BASE + '/hubs/chat')
        .withAutomaticReconnect()
        .build();

      signalRConnection.on('ReceiveMessage', function(message) {
        if (!document.querySelector('[data-msg-id="' + message.id + '"]')) {
          addMessage(message.content, message.role, message.id);
        }
      });

      signalRConnection.on('ConversationUpdated', function(update) {
        if (update.status === 'resolved') {
          isEscalated = false;
          updateResolvedUI();
          disconnectSignalR();
        }
      });

      signalRConnection.on('AgentTyping', function(data) {
        if (data.isTyping) {
          showAgentTyping();
        } else {
          hideAgentTyping();
        }
      });

      await signalRConnection.start();
      await signalRConnection.invoke('JoinConversation', conversationId.toString());
      useSignalR = true;
      stopPolling(); // Stop polling when SignalR is connected
      console.log('SignalR connected for conversation ' + conversationId);
    } catch (e) {
      console.log('SignalR connection failed, using polling fallback:', e);
      signalRConnection = null;
      useSignalR = false;
    }
  }

  function disconnectSignalR() {
    if (signalRConnection) {
      signalRConnection.stop();
      signalRConnection = null;
      useSignalR = false;
    }
  }

  function loadSignalRScript() {
    return new Promise(function(resolve, reject) {
      var script = document.createElement('script');
      script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js';
      script.onload = resolve;
      script.onerror = reject;
      document.head.appendChild(script);
    });
  }

  function showAgentTyping() {
    var existingTyping = container.querySelector('#algora-agent-typing');
    if (existingTyping) return;

    var messagesEl = container.querySelector('#algora-messages');
    var typing = document.createElement('div');
    typing.className = 'algora-typing';
    typing.id = 'algora-agent-typing';
    typing.innerHTML = '<span></span><span></span><span></span>';
    messagesEl.appendChild(typing);
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function hideAgentTyping() {
    var typing = container.querySelector('#algora-agent-typing');
    if (typing) typing.remove();
  }

  function updateEscalatedUI() {
    var humanBtn = container.querySelector('#algora-human-btn');
    if (humanBtn) {
      humanBtn.style.display = 'none';
    }

    var statusEl = container.querySelector('#algora-status');
    if (statusEl) {
      statusEl.textContent = 'Connected to support';
      statusEl.style.display = 'block';
    }
  }

  function updateResolvedUI() {
    var statusEl = container.querySelector('#algora-status');
    if (statusEl) {
      statusEl.textContent = 'Conversation resolved';
    }
  }

  // UI Creation
  function createWidget(cfg) {
    config = cfg;

    // Create container
    container = document.createElement('div');
    container.id = 'algora-chatbot-widget';
    container.innerHTML = getWidgetHTML();
    document.body.appendChild(container);

    // Apply styles
    applyStyles();

    // Add event listeners
    addEventListeners();
  }

  function getWidgetHTML() {
    var pos = config.position === 'bottom-left' ? 'left: 20px;' : 'right: 20px;';

    return `
      <div id="algora-trigger" style="${pos} bottom: 20px;">
        <div class="algora-trigger-btn" style="background: ${config.primaryColor};">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="white">
            <path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H6l-2 2V4h16v12z"/>
          </svg>
          <span class="algora-trigger-text">${config.triggerText || ''}</span>
        </div>
      </div>

      <div id="algora-chat-window" style="${pos} bottom: 90px; display: none;">
        <div class="algora-header" style="background: ${config.headerBackgroundColor || config.primaryColor}; color: ${config.headerTextColor || '#fff'};">
          <div class="algora-header-content">
            ${config.avatarUrl ? `<img src="${config.avatarUrl}" class="algora-avatar" alt="">` : ''}
            <div class="algora-header-text">
              <div class="algora-header-title">${config.headerTitle || 'Chat with us'}</div>
              <div class="algora-header-subtitle">${config.botName}</div>
            </div>
          </div>
          <button class="algora-close-btn" aria-label="Close chat">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
              <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
            </svg>
          </button>
        </div>

        <div class="algora-messages" id="algora-messages"></div>

        <div class="algora-human-bar" id="algora-human-bar">
          <span id="algora-status" style="display: none; color: #22c55e; font-size: 12px;"></span>
          <button id="algora-human-btn" class="algora-human-btn">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
            </svg>
            Talk to Human
          </button>
        </div>

        <div class="algora-input-area">
          <input type="text" id="algora-input" placeholder="${config.placeholderText || 'Type your message...'}" />
          <button id="algora-send-btn" style="background: ${config.primaryColor};">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="white">
              <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/>
            </svg>
          </button>
        </div>

        ${config.showPoweredBy ? `
          <div class="algora-powered-by">
            Powered by <a href="https://algora.app" target="_blank">Algora</a>
          </div>
        ` : ''}
      </div>
    `;
  }

  function applyStyles() {
    var style = document.createElement('style');
    style.textContent = `
      #algora-chatbot-widget * {
        box-sizing: border-box;
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
      }
      #algora-trigger {
        position: fixed;
        z-index: 99999;
      }
      .algora-trigger-btn {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 12px 16px;
        border-radius: 50px;
        cursor: pointer;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        transition: transform 0.2s, box-shadow 0.2s;
      }
      .algora-trigger-btn:hover {
        transform: scale(1.05);
        box-shadow: 0 6px 16px rgba(0,0,0,0.2);
      }
      .algora-trigger-text {
        color: white;
        font-size: 14px;
        font-weight: 500;
      }
      #algora-chat-window {
        position: fixed;
        width: 380px;
        max-width: calc(100vw - 40px);
        height: 500px;
        max-height: calc(100vh - 120px);
        background: white;
        border-radius: 16px;
        box-shadow: 0 8px 32px rgba(0,0,0,0.15);
        display: flex;
        flex-direction: column;
        z-index: 99999;
        overflow: hidden;
      }
      .algora-header {
        padding: 16px;
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
      .algora-header-content {
        display: flex;
        align-items: center;
        gap: 12px;
      }
      .algora-avatar {
        width: 40px;
        height: 40px;
        border-radius: 50%;
      }
      .algora-header-title {
        font-size: 16px;
        font-weight: 600;
      }
      .algora-header-subtitle {
        font-size: 12px;
        opacity: 0.8;
      }
      .algora-close-btn {
        background: none;
        border: none;
        cursor: pointer;
        opacity: 0.8;
        padding: 4px;
      }
      .algora-close-btn:hover {
        opacity: 1;
      }
      .algora-messages {
        flex: 1;
        overflow-y: auto;
        padding: 16px;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }
      .algora-message {
        max-width: 85%;
        padding: 12px 16px;
        border-radius: 16px;
        font-size: 14px;
        line-height: 1.5;
      }
      .algora-message.user {
        align-self: flex-end;
        background: ${config.primaryColor};
        color: white;
        border-bottom-right-radius: 4px;
      }
      .algora-message.assistant {
        align-self: flex-start;
        background: #f1f3f5;
        color: #333;
        border-bottom-left-radius: 4px;
      }
      .algora-message.agent {
        align-self: flex-start;
        background: #dbeafe;
        color: #1e40af;
        border-bottom-left-radius: 4px;
        border-left: 3px solid #3b82f6;
      }
      .algora-message.agent::before {
        content: '👤 Support Agent';
        display: block;
        font-size: 10px;
        font-weight: 600;
        color: #3b82f6;
        margin-bottom: 4px;
      }
      .algora-message.system {
        align-self: center;
        background: #fef3c7;
        color: #92400e;
        font-size: 12px;
        border-radius: 8px;
        max-width: 90%;
        text-align: center;
      }
      .algora-human-bar {
        padding: 8px 12px;
        border-top: 1px solid #eee;
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
      .algora-human-btn {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: 6px 12px;
        background: transparent;
        border: 1px solid #6b7280;
        color: #6b7280;
        border-radius: 16px;
        font-size: 12px;
        cursor: pointer;
        transition: all 0.2s;
      }
      .algora-human-btn:hover {
        background: #f3f4f6;
        border-color: #374151;
        color: #374151;
      }
      .algora-typing {
        display: flex;
        gap: 4px;
        padding: 12px 16px;
        background: #f1f3f5;
        border-radius: 16px;
        border-bottom-left-radius: 4px;
        width: fit-content;
      }
      .algora-typing span {
        width: 8px;
        height: 8px;
        background: #999;
        border-radius: 50%;
        animation: algora-bounce 1.4s infinite ease-in-out;
      }
      .algora-typing span:nth-child(1) { animation-delay: -0.32s; }
      .algora-typing span:nth-child(2) { animation-delay: -0.16s; }
      @keyframes algora-bounce {
        0%, 80%, 100% { transform: scale(0); }
        40% { transform: scale(1); }
      }
      .algora-input-area {
        padding: 12px;
        border-top: 1px solid #eee;
        display: flex;
        gap: 8px;
      }
      #algora-input {
        flex: 1;
        padding: 12px 16px;
        border: 1px solid #ddd;
        border-radius: 24px;
        font-size: 14px;
        outline: none;
      }
      #algora-input:focus {
        border-color: ${config.primaryColor};
      }
      #algora-send-btn {
        width: 44px;
        height: 44px;
        border: none;
        border-radius: 50%;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      #algora-send-btn:hover {
        opacity: 0.9;
      }
      .algora-powered-by {
        padding: 8px;
        text-align: center;
        font-size: 11px;
        color: #999;
        border-top: 1px solid #eee;
      }
      .algora-powered-by a {
        color: ${config.primaryColor};
        text-decoration: none;
      }
      .algora-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin-top: 8px;
      }
      .algora-action-btn {
        padding: 8px 12px;
        background: white;
        border: 1px solid ${config.primaryColor};
        color: ${config.primaryColor};
        border-radius: 16px;
        font-size: 13px;
        cursor: pointer;
      }
      .algora-action-btn:hover {
        background: ${config.primaryColor};
        color: white;
      }
    `;
    document.head.appendChild(style);
  }

  function addEventListeners() {
    var trigger = container.querySelector('.algora-trigger-btn');
    var closeBtn = container.querySelector('.algora-close-btn');
    var input = container.querySelector('#algora-input');
    var sendBtn = container.querySelector('#algora-send-btn');
    var chatWindow = container.querySelector('#algora-chat-window');
    var triggerEl = container.querySelector('#algora-trigger');

    trigger.addEventListener('click', function() {
      toggleChat();
    });

    closeBtn.addEventListener('click', function() {
      toggleChat();
    });

    sendBtn.addEventListener('click', function() {
      handleSend();
    });

    input.addEventListener('keypress', function(e) {
      if (e.key === 'Enter') {
        handleSend();
      }
    });

    // Talk to Human button
    var humanBtn = container.querySelector('#algora-human-btn');
    if (humanBtn) {
      humanBtn.addEventListener('click', function() {
        handleEscalate();
      });
    }
  }

  async function handleEscalate() {
    var humanBtn = container.querySelector('#algora-human-btn');
    if (humanBtn) {
      humanBtn.disabled = true;
      humanBtn.textContent = 'Connecting...';
    }

    var shop = window.AlgoraChatbot.shop;

    // Start conversation if not exists
    if (!conversationId) {
      await startConversation(shop);
    }

    var result = await escalateToHuman('Customer requested human support');

    if (result && result.success) {
      addMessage(result.message, 'system');
      // Try to connect via SignalR for real-time updates, fall back to polling
      await connectSignalR();
      if (!useSignalR) {
        startPolling();
      }
    } else {
      if (humanBtn) {
        humanBtn.disabled = false;
        humanBtn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg> Talk to Human';
      }
      addMessage('Sorry, we could not connect you to an agent. Please try again.', 'system');
    }
  }

  function toggleChat() {
    var chatWindow = container.querySelector('#algora-chat-window');
    var trigger = container.querySelector('#algora-trigger');

    isOpen = !isOpen;
    chatWindow.style.display = isOpen ? 'flex' : 'none';
    trigger.style.display = isOpen ? 'none' : 'block';

    if (isOpen && !conversationId) {
      initConversation();
    }
  }

  async function initConversation() {
    var shop = window.AlgoraChatbot.shop;
    var data = await startConversation(shop);
    if (data && data.messages) {
      data.messages.forEach(function(msg) {
        addMessage(msg.content, msg.role);
      });
    }
  }

  function addMessage(content, role, msgId) {
    var messagesEl = container.querySelector('#algora-messages');
    var msgEl = document.createElement('div');
    msgEl.className = 'algora-message ' + role;
    if (msgId) {
      msgEl.setAttribute('data-msg-id', msgId);
    }
    msgEl.textContent = content;
    messagesEl.appendChild(msgEl);
    messagesEl.scrollTop = messagesEl.scrollHeight;

    // Play notification sound for agent messages if enabled
    if (role === 'agent' && config.enableSoundNotifications) {
      playNotificationSound();
    }
  }

  function playNotificationSound() {
    try {
      var audio = new Audio('data:audio/wav;base64,UklGRl9vT19XQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YU' + Array(300).join('ABC'));
      audio.volume = 0.3;
      audio.play().catch(function() {});
    } catch (e) {}
  }

  function showTyping() {
    var messagesEl = container.querySelector('#algora-messages');
    var typing = document.createElement('div');
    typing.className = 'algora-typing';
    typing.id = 'algora-typing-indicator';
    typing.innerHTML = '<span></span><span></span><span></span>';
    messagesEl.appendChild(typing);
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function hideTyping() {
    var typing = container.querySelector('#algora-typing-indicator');
    if (typing) typing.remove();
  }

  async function handleSend() {
    var input = container.querySelector('#algora-input');
    var message = input.value.trim();
    if (!message) return;

    input.value = '';
    addMessage(message, 'user');
    showTyping();

    var shop = window.AlgoraChatbot.shop;
    var response = await sendMessage(shop, message);

    hideTyping();

    if (response && response.success) {
      addMessage(response.response, 'assistant');

      if (response.suggestedActions && response.suggestedActions.length > 0) {
        addActions(response.suggestedActions);
      }
    } else {
      addMessage('Sorry, I encountered an error. Please try again.', 'assistant');
    }
  }

  function addActions(actions) {
    var messagesEl = container.querySelector('#algora-messages');
    var actionsEl = document.createElement('div');
    actionsEl.className = 'algora-actions';

    actions.forEach(function(action) {
      var btn = document.createElement('button');
      btn.className = 'algora-action-btn';
      btn.textContent = action.label;
      btn.addEventListener('click', function() {
        if (action.type === 'link') {
          window.open(action.value, '_blank');
        } else {
          var input = container.querySelector('#algora-input');
          input.value = action.label;
          handleSend();
        }
      });
      actionsEl.appendChild(btn);
    });

    messagesEl.appendChild(actionsEl);
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  // Initialize
  async function init(options) {
    if (!options || !options.shop) {
      console.error('AlgoraChatbot: shop parameter is required');
      return;
    }

    window.AlgoraChatbot.shop = options.shop;
    sessionId = getSessionId();
    visitorId = getVisitorId();

    // Determine API base URL
    API_BASE = options.apiUrl || (window.location.hostname.includes('localhost')
      ? 'https://localhost:5001'
      : 'https://chatbot.algora.app');

    // Fetch config and create widget
    var cfg = await fetchConfig(options.shop);
    createWidget(cfg);

    // Auto-open if configured
    if (cfg.autoOpenOnFirstVisit && !sessionStorage.getItem('algora_opened')) {
      setTimeout(function() {
        toggleChat();
        sessionStorage.setItem('algora_opened', 'true');
      }, (cfg.autoOpenDelaySeconds || 5) * 1000);
    }
  }

  // Process queued calls
  window.AlgoraChatbot = window.AlgoraChatbot || function() {
    (window.AlgoraChatbot.q = window.AlgoraChatbot.q || []).push(arguments);
  };

  var queue = window.AlgoraChatbot.q || [];
  window.AlgoraChatbot = init;
  window.AlgoraChatbot.q = queue;

  queue.forEach(function(args) {
    if (args[0] === 'init') {
      init(args[1]);
    }
  });

})(window, document);
