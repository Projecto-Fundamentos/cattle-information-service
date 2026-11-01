package com.smartfarm.iam.interfaces;
import com.smartfarm.iam.application.ApiException;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.*;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.*;
import java.util.*;
@RestControllerAdvice
public class ApiExceptionHandler {
    @ExceptionHandler(ApiException.class)
    ResponseEntity<ProblemDetail> business(ApiException ex) { return problem(ex.status(), ex.code(), ex.getMessage()); }
    @ExceptionHandler(MethodArgumentNotValidException.class)
    ResponseEntity<ProblemDetail> validation(MethodArgumentNotValidException ex) {
        var response = problem(HttpStatus.BAD_REQUEST, "VALIDATION_ERROR", "Request validation failed");
        Map<String,String> fields = new TreeMap<>();
        ex.getBindingResult().getFieldErrors().forEach(e -> fields.putIfAbsent(e.getField(), e.getDefaultMessage()));
        response.getBody().setProperty("errors", fields);
        return response;
    }
    @ExceptionHandler(HttpMessageNotReadableException.class)
    ResponseEntity<ProblemDetail> malformed() { return problem(HttpStatus.BAD_REQUEST, "INVALID_JSON", "Malformed JSON or incompatible field types"); }
    @ExceptionHandler(DataIntegrityViolationException.class)
    ResponseEntity<ProblemDetail> conflict() {
        // Handles competing registrations after the pre-check; transaction has rolled back.
        return problem(HttpStatus.CONFLICT, "REGISTRATION_CONFLICT", "Account or ranch conflicts with existing data");
    }
    private ResponseEntity<ProblemDetail> problem(HttpStatus status, String code, String message) {
        ProblemDetail detail = ProblemDetail.forStatusAndDetail(status, message);
        detail.setTitle(status.getReasonPhrase()); detail.setProperty("code", code);
        return ResponseEntity.status(status).body(detail);
    }
}
