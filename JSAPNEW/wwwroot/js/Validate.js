// Validation functions
        function validateField(field) {
            const fieldId = field.id || field.name;
            const formGroup = field.closest('.form-group');
            const errorElement = document.getElementById(`${fieldId}-error`);
            let isValid = true;
            let errorMessage = '';

            // Remove previous error styling
            if (formGroup) {
                formGroup.classList.remove('error');
            }
            if (errorElement) {
                errorElement.style.display = 'none';
            }

            // Check if field is required and empty
            if (field.hasAttribute('required') && !field.value.trim()) {
                isValid = false;
                errorMessage = 'This field is required';
            }
            // Check pattern validation
            else if (field.pattern && field.value && !new RegExp(field.pattern).test(field.value)) {
                isValid = false;
                errorMessage = 'Invalid format';
            }
            // Check minlength
            else if (field.minLength && field.value.length < field.minLength && field.value.length > 0) {
                isValid = false;
                errorMessage = `Minimum ${field.minLength} characters required`;
            }
            // Check maxlength
            else if (field.maxLength && field.value.length > field.maxLength) {
                isValid = false;
                errorMessage = `Maximum ${field.maxLength} characters allowed`;
            }
            // Special validations
            else if (field.type === 'email' && field.value && !isValidEmail(field.value)) {
                isValid = false;
                errorMessage = 'Please enter a valid email address';
            }

            // Show error if validation failed
            if (!isValid) {
                if (formGroup) {
                    formGroup.classList.add('error');
                }
                if (errorElement) {
                    errorElement.textContent = errorMessage;
                    errorElement.style.display = 'block';
                }
            }

            return isValid;
        }

        function isValidEmail(email) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(email);
        }// Validation functions
        function validateField(field) {
            const fieldId = field.id || field.name;
            const formGroup = field.closest('.form-group');
            const errorElement = document.getElementById(`${fieldId}-error`);
            let isValid = true;
            let errorMessage = '';

            // Remove previous error styling
            if (formGroup) {
                formGroup.classList.remove('error');
            }
            if (errorElement) {
                errorElement.style.display = 'none';
            }

            // Check if field is required and empty
            if (field.hasAttribute('required') && !field.value.trim()) {
                isValid = false;
                errorMessage = 'This field is required';
            }
            // Check pattern validation
            else if (field.pattern && field.value && !new RegExp(field.pattern).test(field.value)) {
                isValid = false;
                errorMessage = 'Invalid format';
            }
            // Check minlength
            else if (field.minLength && field.value.length < field.minLength && field.value.length > 0) {
                isValid = false;
                errorMessage = `Minimum ${field.minLength} characters required`;
            }
            // Check maxlength
            else if (field.maxLength && field.value.length > field.maxLength) {
                isValid = false;
                errorMessage = `Maximum ${field.maxLength} characters allowed`;
            }
            // Special validations
            else if (field.type === 'email' && field.value && !isValidEmail(field.value)) {
                isValid = false;
                errorMessage = 'Please enter a valid email address';
            }

            // Show error if validation failed
            if (!isValid) {
                if (formGroup) {
                    formGroup.classList.add('error');
                }
                if (errorElement) {
                    errorElement.textContent = errorMessage;
                    errorElement.style.display = 'block';
                }
            }

            return isValid;
        }

        function isValidEmail(email) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(email);
        }