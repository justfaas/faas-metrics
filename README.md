# FaaS Metrics Server

This project imports metrics from Prometheus and exports them as Kubernetes custom metrics.

## Metrics

The following proxy metrics are exported

- faas_proxy_requests_total{service}
- faas_proxy_requests_per_second{service}

These metrics are defined with a `metrics.yml` configuration file, deployed with a config map.

## Prometheus Sources

If no source is configured, the metrics server will look for a running instance in `faas` namespace; this is the default behaviour.

A customized configuration is possible by passing one or more `--source=prometheus_url` arguments in the container template ~~or by using a config map in conjunction with a `--config=config.yml` argument~~.

## TLS

Currently the application is generating a self-signed certificate with a 90 day validity. This validity is verified every time the application starts and when close to expiration date, the certificate is renewed. The validity is also verified when the application is running, since there's nothing stopping the application to run without restarts for several days/months; when close to expiration date, the application returns a non-healthy status, triggering a restart and creating a new certificate.

This works, but there's probably a better way of doing this. Maybe using cert-manager, if it's available?
