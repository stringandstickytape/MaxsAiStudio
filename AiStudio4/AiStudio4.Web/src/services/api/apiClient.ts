import axios from 'axios';
import { create } from 'zustand';
import { webSocketService } from '../websocket/WebSocketService';


export const apiClient = axios.create({
  baseURL: '/',
  headers: {
    'Content-Type': 'application/json',
  },
});


apiClient.interceptors.request.use((config) => {
    console.log('[ApiClient Debug] Request intercepted:', config.method?.toUpperCase(), config.url);
    console.log('[ApiClient Debug] Request headers:', config.headers);
    console.log('[ApiClient Debug] Request data:', config.data);
    
    const clientId = webSocketService.getClientId();
    if (clientId) {
        config.headers['X-Client-Id'] = clientId;
        console.log('[ApiClient Debug] Added client ID to headers:', clientId);
    }
    return config;
});


apiClient.interceptors.response.use(
  (response) => {
    console.log('[ApiClient Debug] Response received:', response.status, response.config.url);
    console.log('[ApiClient Debug] Response data:', response.data);
    return response;
  },
  (error) => {
    console.log('[ApiClient Debug] Response error:', error);
    
    const errorResponse = {
      message: error.message || 'An unknown error occurred',
      status: error.response?.status,
      data: error.response?.data,
    };

    console.error('[ApiClient Debug] Formatted error response:', errorResponse);
    
    return Promise.reject(errorResponse);
  },
);



export async function updateMessage(params: { convId: string; messageId: string; content: string }) {
  try {
    const response = await apiClient.post('/api/updateMessage', params);
    return response.data;
  } catch (error) {
    console.error('Error updating message:', error);
    throw error;
  }
}

export async function deleteMessageWithDescendants(params: { convId: string; messageId: string }) {
  try {
    const response = await apiClient.post('/api/deleteMessageWithDescendants', params);
    return response.data;
  } catch (error) {
    console.error('Error deleting message with descendants:', error);
    throw error;
  }
}

export async function deleteConv(params: { convId: string }) {
  try {
    const response = await apiClient.post('/api/deleteConv', params);
    return response.data;
  } catch (error) {
    console.error('Error deleting conversation:', error);
    throw error;
  }
}

export async function cancelRequest(params: { convId: string; messageId: string }) {
  try {
    const response = await apiClient.post('/api/cancelRequest', params);
    return response.data;
  } catch (error) {
    console.error('Error cancelling request:', error);
    throw error;
  }
}

export async function saveCodeBlockAsFile(params: { content: string; suggestedFilename: string }) {
  try {
    const response = await apiClient.post('/api/saveCodeBlockAsFile', params);
    return response.data;
  } catch (error) {
    console.error('Error saving code block as file:', error);
    throw error;
  }
}