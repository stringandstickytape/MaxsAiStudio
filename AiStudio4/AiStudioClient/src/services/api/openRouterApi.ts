import axios from 'axios';

export interface OpenRouterPricing {
  prompt: string;
  completion: string;
  request: string;
  image: string;
}

export interface OpenRouterArchitecture {
  modality: string;
  tokenizer: string;
  instruct_type: string | null;
  input_modalities?: string[];
}

export interface OpenRouterModel {
  id: string;
  name: string;
  description: string;
  context_length: number;
  pricing: OpenRouterPricing;
  architecture: OpenRouterArchitecture;
  supported_parameters?: string[];
}

export interface OpenRouterApiResponse {
  data: OpenRouterModel[];
}

const OPENROUTER_API_URL = 'https://openrouter.ai/api/v1/models';

export async function fetchOpenRouterModels(apiKey: string): Promise<OpenRouterApiResponse> {
  if (!apiKey) {
    throw new Error('OpenRouter API key is required.');
  }

  try {
    const response = await axios.get<OpenRouterApiResponse>(OPENROUTER_API_URL, {
      headers: {
        'Authorization': `Bearer ${apiKey}`,
        'HTTP-Referer': 'https://github.com/stringandstickytape/MaxsAiStudio',
        'X-Title': 'Max\'s AI Studio',
      },
    });
    return response.data;
  } catch (error) {
    console.error("Failed to fetch models from OpenRouter:", error);
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      throw new Error('Authentication failed. Please check your OpenRouter API key.');
    }
    throw new Error('Failed to fetch models from OpenRouter. Please check your network connection.');
  }
}