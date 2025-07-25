#!/bin/bash

# Claude Code startup script for AWS Bedrock integration
# This script configures AWS credentials and environment variables for Claude Code

# Use Novita and Qwen Coder
export ANTHROPIC_BASE_URL="https://api.novita.ai/anthropic"
export ANTHROPIC_AUTH_TOKEN="sk_fDsXSKvtEtBbk2iomAQnx8HxDkzRyoNu_kOPusWvmBM"
export ANTHROPIC_MODEL="qwen/qwen3-coder-480b-a35b-instruct"
export ANTHROPIC_SMALL_FAST_MODEL="qwen/qwen3-coder-480b-a35b-instruct"
#export ANTHROPIC_MODEL="moonshotai/kimi-k2-instruct"
#export ANTHROPIC_SMALL_FAST_MODEL="moonshotai/kimi-k2-instruct"

# Launch Claude Code
claude -c
