import socket
import struct
import time
import random
import hashlib

def print_hex(data):
    return ' '.join(f'{b:02x}' for b in data)

def bit_pack(data):
    """Pack data into a bit-packed format"""
    result = bytearray()
    current_byte = 0
    bit_count = 0
    
    for bit in data:
        current_byte = (current_byte << 1) | bit
        bit_count += 1
        
        if bit_count == 8:
            result.append(current_byte)
            current_byte = 0
            bit_count = 0
    
    if bit_count > 0:
        current_byte <<= (8 - bit_count)
        result.append(current_byte)
    
    return bytes(result)

# Create UDP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Server address
server_address = ('127.0.0.1', 7777)

# First, send a connectionless packet to initiate the handshake
# Format: [HandshakeBit (1 bit)][RestartBit (1 bit)][SecretId (1 bit)][Timestamp (8 bytes)][Cookie (20 bytes)]
handshake_bit = 1
restart_bit = 0
secret_id = 0
timestamp = 0.0  # Must be 0.0 for initial connection
cookie = bytes([0] * 20)  # Initial empty cookie

# Pack the bits
bits = [handshake_bit, restart_bit, secret_id]
packed_bits = bit_pack(bits)

# Pack the full message
initial_packet = packed_bits + struct.pack('d', timestamp) + cookie

print(f"Sending initial packet: {print_hex(initial_packet)}")
print(f"Handshake bit: {handshake_bit}")
print(f"Restart bit: {restart_bit}")
print(f"Secret ID: {secret_id}")
print(f"Timestamp: {timestamp}")

# Send the initial packet
sock.sendto(initial_packet, server_address)
print("Initial packet sent")

# Wait for challenge response
sock.settimeout(5.0)
try:
    data, addr = sock.recvfrom(1024)
    print(f"Received challenge from {addr}: {print_hex(data)}")
    
    # Parse the challenge response
    if len(data) >= 29:  # Minimum size for handshake bit + secret id + timestamp + cookie
        # First byte contains handshake bit and restart bit
        first_byte = data[0]
        handshake_bit = (first_byte >> 7) & 1
        restart_bit = (first_byte >> 6) & 1
        secret_id = (first_byte >> 5) & 1
        
        # Parse the rest of the packet
        timestamp = struct.unpack('d', data[1:9])[0]
        server_cookie = data[9:29]
        
        print(f"Response handshake bit: {handshake_bit}")
        print(f"Response restart bit: {restart_bit}")
        print(f"Response secret ID: {secret_id}")
        print(f"Response timestamp: {timestamp}")
        print(f"Response cookie: {print_hex(server_cookie)}")
        
        # Send the challenge response
        # Format: [HandshakeBit (1 bit)][RestartBit (1 bit)][SecretId (1 bit)][Timestamp (8 bytes)][Cookie (20 bytes)]
        response_bits = [1, 0, secret_id]  # Handshake bit = 1, Restart bit = 0, use server's secret ID
        response_packed_bits = bit_pack(response_bits)
        response_packet = response_packed_bits + struct.pack('d', timestamp) + server_cookie
        
        print(f"Sending challenge response: {print_hex(response_packet)}")
        sock.sendto(response_packet, server_address)
        
        # Wait for final handshake response
        data, addr = sock.recvfrom(1024)
        print(f"Received final response from {addr}: {print_hex(data)}")
        
        if len(data) >= 6:  # Minimum size for channel + message type + success flag
            channel_index = data[0]
            message_type = data[1]
            success = bool(data[2])
            player_id = struct.unpack('i', data[3:7])[0]
            welcome_msg = data[7:].decode('utf-8').rstrip('\0')
            
            print(f"Channel index: {channel_index}")
            print(f"Message type: {message_type}")
            print(f"Success: {success}")
            print(f"Player ID: {player_id}")
            print(f"Welcome message: {welcome_msg}")
        else:
            print("Received invalid final response format")
    else:
        print("Received invalid challenge response format")
        
except socket.timeout:
    print("No response received")

# Keep socket open for a moment to ensure message is processed
time.sleep(1)
sock.close()