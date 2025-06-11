# Sophic Journal - System Architecture Overview

**Version:** 1.0  
**Date:** June 11, 2025  
**Document Type:** Technical Architecture Specification  
**Classification:** QA Testing Reference

-----

## Architecture Philosophy

Sophic Journal implements a **zero-trust, privacy-first architecture** where user data never persists on client devices. This design philosophy drives every architectural decision, from authentication mechanisms to data flow patterns, ensuring enterprise-grade security while maintaining responsive user experience.

**Core Principles:**

- **Data Sovereignty:** Users maintain complete control over personal information
- **Zero Client Persistence:** No identifiable data stored on user devices
- **Defense in Depth:** Multiple security layers prevent unauthorized access
- **Graceful Degradation:** System remains functional during network disruptions
- **Scalable Foundation:** Architecture supports growth from personal to enterprise use

-----

## System Component Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│                 │    │                 │    │                 │
│   Client App    │◄──►│  Network Layer  │◄──►│   API Server    │
│   (Avalonia)    │    │   (SSH/SSL)     │    │ (Raspberry Pi)  │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
        │                       │                       │
        ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│                 │    │                 │    │                 │
│   UI/UX Layer   │    │ Security Stack  │    │  Data Storage   │
│   User Interface│    │ Authentication  │    │   Encrypted     │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

-----

## Client Application Architecture

### Technology Stack

**Framework:** Avalonia UI (.NET Core)

- Cross-platform compatibility (Windows, macOS, Linux)
- Native performance with managed code benefits
- MVVM architecture pattern implementation
- Reactive UI patterns for responsive user experience

### Component Breakdown

**Presentation Layer (Views):**

```
MainWindow.xaml          # Primary application container
├── EntryListView.xaml   # Journal entry browsing interface
├── EntryEditorView.xaml # Content creation and editing
├── ReflectionView.xaml  # Mood tracking and analysis
└── SettingsView.xaml    # Configuration and preferences
```

**Business Logic Layer (ViewModels):**

```
ViewModels/
├── MainWindowViewModel.cs    # Application state management
├── EntryListViewModel.cs     # Entry browsing and filtering
├── EntryEditorViewModel.cs   # Content editing operations
├── ReflectionViewModel.cs    # Mood tracking logic
└── SettingsViewModel.cs      # Configuration management
```

**Data Access Layer (Services):**

```
Services/
├── ApiClient.cs              # HTTP communication wrapper
├── AuthenticationService.cs  # SSH/SSL credential management
├── CacheService.cs          # Temporary session data (memory only)
├── NetworkMonitor.cs        # Connection status tracking
└── ErrorHandler.cs          # Exception and error management
```

### Security Implementation

**Zero Persistence Policy:**

- No SQLite databases or local files
- No application settings containing sensitive data
- Session data stored in memory only (cleared on exit)
- Automatic memory cleanup on authentication failure

**Authentication Flow:**

1. SSH key validation with remote server
1. SSL certificate verification for secure channel
1. JWT token reception and memory-only storage
1. Automatic token refresh and session management

-----

## Server Infrastructure Architecture

### Hardware Platform

**Raspberry Pi Configuration:**

- ARM-based processing for power efficiency
- Dedicated network interface for security isolation
- External storage with encryption at rest
- Hardware security module integration capability

### Software Stack

**Operating System:** Debian Linux (Raspberry Pi OS)

- Minimal installation for reduced attack surface
- Automatic security updates enabled
- Custom firewall configuration (UFW)
- SSH hardening with key-only authentication

**API Server Framework:**

```
/opt/sophic-journal/
├── api/
│   ├── controllers/         # REST endpoint implementations
│   ├── middleware/          # Authentication and logging
│   ├── models/             # Data structure definitions
│   └── services/           # Business logic implementation
├── config/
│   ├── ssl/                # Certificate management
│   ├── ssh/                # Key authentication setup
│   └── app.conf           # Application configuration
└── data/
    ├── encrypted/          # User data storage (encrypted)
    └── logs/              # Security and access logging
```

### API Endpoint Structure

**Authentication Endpoints:**

```
POST /api/auth/login        # SSH key authentication
POST /api/auth/refresh      # Token renewal
POST /api/auth/logout       # Session termination
GET  /api/auth/status       # Session validation
```

**Journal Management Endpoints:**

```
GET    /api/journal/entries          # Retrieve user entries
POST   /api/journal/entries          # Create new entry
PUT    /api/journal/entries/{id}     # Update existing entry
DELETE /api/journal/entries/{id}     # Remove entry
GET    /api/journal/search?q={query} # Search functionality
```

**Reflection and Analytics Endpoints:**

```
GET  /api/reflection/moods           # Mood tracking data
POST /api/reflection/entries         # New reflection entry
GET  /api/reflection/insights        # Generated insights
GET  /api/reflection/trends          # Historical analysis
```

-----

## Network Security Architecture

### Encryption Stack

**Transport Layer Security:**

- TLS 1.3 for all HTTP communications
- Perfect Forward Secrecy (PFS) implementation
- Strong cipher suite configuration
- Certificate pinning for enhanced security

**SSH Layer Security:**

- RSA 4096-bit or Ed25519 key pairs
- SSH protocol version 2 exclusively
- Disabled password authentication
- Connection rate limiting and intrusion detection

### Network Topology

```
Internet
    │
    ▼
┌─────────────────┐
│   Firewall      │ ◄── Port 22 (SSH), 443 (HTTPS) only
│   (UFW/iptables)│
└─────────────────┘
    │
    ▼
┌─────────────────┐
│  Raspberry Pi   │ ◄── API Server (internal network)
│  192.168.1.100  │
└─────────────────┘
    │
    ▼
┌─────────────────┐
│ Encrypted Data  │ ◄── AES-256 encryption at rest
│   Storage       │
└─────────────────┘
```

### Security Monitoring

**Intrusion Detection:**

- Failed authentication attempt logging
- Unusual access pattern detection
- Automated IP blocking for repeated failures
- Real-time security alert notifications

**Audit Trail:**

- Complete API access logging
- User action tracking and timestamps
- Data modification history
- Security event correlation and analysis

-----

## Data Flow Architecture

### Entry Creation Workflow

```
User Input → Client Validation → Network Transmission → Server Validation → Data Storage
     ▲              │                      │                    │              │
     │              ▼                      ▼                    ▼              ▼
UI Response ← Error Handling ← SSL Encryption ← Authentication ← Encryption
```

**Step-by-Step Process:**

1. **User Input:** Content entered in Avalonia UI
1. **Client Validation:** Basic format and length validation
1. **Authentication Check:** Verify valid session token
1. **SSL Transmission:** Encrypted data sent to server
1. **Server Authentication:** Token validation and user verification
1. **Data Processing:** Content sanitization and validation
1. **Encrypted Storage:** AES-256 encryption before database storage
1. **Response Generation:** Success confirmation with entry metadata
1. **Client Update:** UI refresh with new entry information

### Authentication Data Flow

```
Client                    Network                    Server
  │                         │                         │
  │── SSH Key ──────────────│─────────────────────────│→ Key Validation
  │                         │                         │
  │                         │← SSL Certificate ───────│
  │                         │                         │
  │← JWT Token ─────────────│─────────────────────────│
  │                         │                         │
  │── API Requests ─────────│→ Token Validation ──────│
  │   (with Bearer token)   │                         │
```

-----

## Scalability Considerations

### Current Architecture Capacity

**Single Raspberry Pi Limitations:**

- Concurrent users: 50-100 typical usage
- Data storage: Up to 1TB with external drives
- Network throughput: 100Mbps Ethernet limitation
- Processing power: ARM Cortex-A72 quad-core constraints

### Horizontal Scaling Path

**Multi-Node Architecture (Future Enhancement):**

```
Load Balancer
     │
     ├── Pi Node 1 (Primary)
     ├── Pi Node 2 (Replica)
     └── Pi Node 3 (Replica)
         │
         └── Shared Database Cluster
```

**Database Scaling Strategy:**

- SQLite for single-node simplicity
- PostgreSQL migration path for multi-node
- Data partitioning by user for horizontal scaling
- Backup and replication strategies

-----

## Monitoring and Observability

### System Health Metrics

**Performance Indicators:**

- API response time percentiles (P50, P95, P99)
- Database query performance
- Memory and CPU utilization
- Network bandwidth consumption
- Storage capacity and growth trends

**Availability Metrics:**

- Service uptime percentage
- Authentication success rates
- Error rate by endpoint
- Network connectivity status

### Logging Architecture

**Application Logs:**

```
/var/log/sophic-journal/
├── api.log              # API request/response logging
├── auth.log             # Authentication events
├── error.log            # Application errors and exceptions
├── performance.log      # Response time and resource usage
└── security.log         # Security events and alerts
```

**Log Retention Policy:**

- Security logs: 1 year retention
- Performance logs: 90 days retention
- Application logs: 30 days retention
- Error logs: 180 days retention

-----

## Disaster Recovery Architecture

### Backup Strategy

**Data Backup:**

- Daily encrypted backups to external storage
- Weekly offsite backup synchronization
- Point-in-time recovery capability
- Automated backup verification and testing

**Configuration Backup:**

- SSH key and SSL certificate backup
- Application configuration versioning
- Database schema and migration scripts
- Infrastructure-as-code documentation

### Recovery Procedures

**Recovery Time Objectives (RTO):**

- Hardware failure: < 4 hours
- Data corruption: < 2 hours
- Security breach: < 1 hour (isolation)
- Configuration loss: < 30 minutes

**Recovery Point Objectives (RPO):**

- User data: < 24 hours (daily backups)
- Configuration: < 1 hour (real-time sync)
- System state: < 4 hours (configuration snapshots)

-----

## Security Architecture Assessment

### Threat Model Analysis

**High-Priority Threats:**

1. **Data Interception:** Mitigated by end-to-end encryption
1. **Authentication Bypass:** Prevented by multi-layer security
1. **Data Exfiltration:** Limited by zero client persistence
1. **Denial of Service:** Managed by rate limiting and monitoring

**Medium-Priority Threats:**

1. **Physical Device Access:** Mitigated by encryption at rest
1. **Network Intrusion:** Detected by monitoring systems
1. **Social Engineering:** Reduced by technical controls
1. **Supply Chain Attacks:** Monitored through update processes

### Compliance Considerations

**Privacy Regulations:**

- GDPR compliance through data sovereignty
- Right to erasure through secure deletion
- Data portability through export functionality
- Consent management through explicit user controls

**Security Standards:**

- OWASP security guidelines implementation
- Industry standard encryption protocols
- Regular security assessment and penetration testing
- Vulnerability management and patching procedures

-----

## Testing Implications

### Architecture-Specific Testing Requirements

**Distributed System Testing:**

- Network partition scenarios
- Component isolation testing
- Inter-service communication validation
- Data consistency across network boundaries

**Security Testing Focus Areas:**

- Authentication mechanism penetration testing
- Encryption implementation validation
- Session management security assessment
- Input validation and sanitization testing

**Performance Testing Considerations:**

- Network latency impact assessment
- Concurrent user load simulation
- Data transfer optimization validation
- Resource utilization under load

### Quality Assurance Integration Points

**Automated Testing Hooks:**

- API endpoint testing infrastructure
- Database state validation utilities
- Network simulation and failure injection
- Security scanning and vulnerability assessment

This architecture documentation provides the foundation for comprehensive quality assurance testing, ensuring all system components and interactions are thoroughly validated for security, performance, and reliability.