#!/usr/bin/env python3
"""
OpenAI Python Bridge for AiStudio4
Long-running process that handles multiple chat completion requests via stdin/stdout.
Requires: pip install openai>=1.0.0
"""

import sys
import json
import os
import traceback
from typing import Dict, List, Any, Optional

try:
    from openai import OpenAI
except ImportError:
    print(json.dumps({
        "type": "error",
        "message": "OpenAI package not found. Run: pip install openai",
        "error_code": "MISSING_OPENAI_PACKAGE"
    }), flush=True)
    sys.exit(1)

class OpenAIBridge:
    def __init__(self):
        self.clients = {}  # Cache clients by (api_key, base_url) to avoid recreating
        
    def get_client(self, api_key: str, base_url: Optional[str] = None) -> OpenAI:
        """Get or create OpenAI client for the given credentials"""
        cache_key = (api_key, base_url)
        if cache_key not in self.clients:
            self.clients[cache_key] = OpenAI(
                api_key=api_key,
                base_url=base_url if base_url and base_url.strip() else None
            )
        return self.clients[cache_key]
    
    def process_request(self, request_data: Dict[str, Any]) -> None:
        """Process a single chat completion request"""
        try:
            # Extract and validate request parameters
            api_key = request_data.get("api_key")
            if not api_key:
                raise ValueError("API key is required")
                
            base_url = request_data.get("base_url")
            model = request_data.get("model")
            messages = request_data.get("messages", [])
            temperature = request_data.get("temperature", 0.7)
            max_tokens = request_data.get("max_tokens")
            top_p = request_data.get("top_p")
            tools = request_data.get("tools", [])
            tool_choice = request_data.get("tool_choice", "auto")
            parallel_tool_calls = request_data.get("parallel_tool_calls", True)
            web_search_options = request_data.get("web_search_options")
            stream = request_data.get("stream", True)
            
            # Get client for this request
            client = self.get_client(api_key, base_url)
            
            # Prepare request parameters
            request_params = {
                "model": model,
                "messages": messages,
                "temperature": temperature,
                "stream": stream
            }
            
            # Add optional parameters
            if max_tokens:
                request_params["max_tokens"] = max_tokens
            if top_p:
                request_params["top_p"] = top_p
            if tools:
                request_params["tools"] = tools
                request_params["tool_choice"] = tool_choice
                request_params["parallel_tool_calls"] = parallel_tool_calls
            if web_search_options is not None:
                request_params["web_search_options"] = web_search_options
            
            # Make the API call
            if stream:
                self._handle_streaming_response(client, request_params)
            else:
                self._handle_non_streaming_response(client, request_params)
                
        except Exception as e:
            self._send_error(f"Request processing error: {str(e)}", traceback.format_exc())
    
    def _handle_streaming_response(self, client: OpenAI, request_params: Dict[str, Any]) -> None:
        """Handle streaming chat completion"""
        try:
            # Add stream options for usage tracking
            request_params["stream_options"] = {"include_usage": True}
            
            stream = client.chat.completions.create(**request_params)
            
            full_content = ""
            tool_calls_accumulator = {}
            finish_reason = None
            
            for chunk in stream:
                choice = chunk.choices[0] if chunk.choices else None
                if not choice:
                    continue
                
                delta = choice.delta
                
                # Handle content streaming
                if delta.content:
                    full_content += delta.content
                    self._send_chunk("content", delta.content)
                
                # Handle tool calls streaming
                if delta.tool_calls:
                    for tool_call in delta.tool_calls:
                        index = tool_call.index
                        
                        if index not in tool_calls_accumulator:
                            tool_calls_accumulator[index] = {
                                "id": tool_call.id or "",
                                "type": tool_call.type or "function",
                                "function": {
                                    "name": tool_call.function.name or "",
                                    "arguments": ""
                                }
                            }
                        
                        # Update tool call data
                        if tool_call.id:
                            tool_calls_accumulator[index]["id"] = tool_call.id
                        if tool_call.type:
                            tool_calls_accumulator[index]["type"] = tool_call.type
                        if tool_call.function.name:
                            tool_calls_accumulator[index]["function"]["name"] = tool_call.function.name
                        if tool_call.function.arguments:
                            tool_calls_accumulator[index]["function"]["arguments"] += tool_call.function.arguments
                        
                        # Send tool call progress
                        self._send_chunk("tool_call_progress", {
                            "index": index,
                            "id": tool_call.id,
                            "function_name": tool_call.function.name,
                            "arguments_fragment": tool_call.function.arguments
                        })
                
                # Handle finish reason
                if choice.finish_reason:
                    finish_reason = choice.finish_reason
                
                # Handle usage info (comes in the last chunk)
                if hasattr(chunk, 'usage') and chunk.usage:
                    usage_info = {
                        "input_tokens": chunk.usage.prompt_tokens,
                        "output_tokens": chunk.usage.completion_tokens,
                        "total_tokens": chunk.usage.total_tokens
                    }
                    
                    self._send_final_response(
                        success=True,
                        content=full_content,
                        tool_calls=list(tool_calls_accumulator.values()) if tool_calls_accumulator else None,
                        finish_reason=finish_reason or "stop",
                        usage=usage_info
                    )
                    return
            
            # Fallback if no usage info received
            self._send_final_response(
                success=True,
                content=full_content,
                tool_calls=list(tool_calls_accumulator.values()) if tool_calls_accumulator else None,
                finish_reason=finish_reason or "stop"
            )
            
        except Exception as e:
            self._send_error(f"Streaming error: {str(e)}", traceback.format_exc())
    
    def _handle_non_streaming_response(self, client: OpenAI, request_params: Dict[str, Any]) -> None:
        """Handle non-streaming chat completion"""
        try:
            response = client.chat.completions.create(**request_params)
            choice = response.choices[0]
            
            usage_info = {
                "input_tokens": response.usage.prompt_tokens,
                "output_tokens": response.usage.completion_tokens,
                "total_tokens": response.usage.total_tokens
            } if response.usage else None
            
            tool_calls = None
            if choice.message.tool_calls:
                tool_calls = [
                    {
                        "id": tc.id,
                        "type": tc.type,
                        "function": {
                            "name": tc.function.name,
                            "arguments": tc.function.arguments
                        }
                    }
                    for tc in choice.message.tool_calls
                ]
            
            self._send_final_response(
                success=True,
                content=choice.message.content or "",
                tool_calls=tool_calls,
                finish_reason=choice.finish_reason,
                usage=usage_info
            )
            
        except Exception as e:
            self._send_error(f"Non-streaming error: {str(e)}", traceback.format_exc())
    
    def _send_chunk(self, chunk_type: str, data: Any) -> None:
        """Send a streaming chunk"""
        response = {
            "type": "chunk",
            "chunk_type": chunk_type,
            "data": data
        }
        print(json.dumps(response), flush=True)
    
    def _send_final_response(self, success: bool, content: str = "", 
                           tool_calls: Optional[List[Dict]] = None,
                           finish_reason: str = "stop",
                           usage: Optional[Dict] = None) -> None:
        """Send final response"""
        response = {
            "type": "end",
            "success": success,
            "content": content,
            "tool_calls": tool_calls,
            "finish_reason": finish_reason,
            "token_usage": usage or {"input_tokens": 0, "output_tokens": 0, "total_tokens": 0}
        }
        print(json.dumps(response), flush=True)
    
    def _send_error(self, message: str, traceback_info: str = "") -> None:
        """Send error response"""
        response = {
            "type": "error",
            "message": message,
            "traceback": traceback_info
        }
        print(json.dumps(response), flush=True)

def main():
    """Main loop to process multiple requests from stdin"""
    bridge = OpenAIBridge()
    
    # Send ready signal
    print(json.dumps({"type": "ready", "message": "OpenAI bridge ready"}), flush=True)
    
    while True:
        try:
            line = sys.stdin.readline()
            if not line:
                break  # Exit if stdin is closed
            
            line = line.strip()
            if not line:
                continue
            
            # Handle special commands
            if line == "PING":
                print(json.dumps({"type": "pong"}), flush=True)
                continue
            elif line == "EXIT":
                break
                
            request_data = json.loads(line)
            bridge.process_request(request_data)
            
        except json.JSONDecodeError as e:
            bridge._send_error(f"JSON Decode Error: {str(e)}")
        except KeyboardInterrupt:
            break
        except Exception as e:
            bridge._send_error(f"Unexpected error: {str(e)}", traceback.format_exc())

if __name__ == "__main__":
    main()