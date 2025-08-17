# YOLOv8 Model for Sperm Detection - README

This directory should contain the YOLOv8 model files for sperm detection in CASA analysis.

## Required Files

### 1. yolov8n_sperm.onnx
- **Description**: Custom-trained YOLOv8 model for sperm detection
- **Format**: ONNX (Open Neural Network Exchange)
- **Size**: Approximately 6-25 MB depending on model size
- **Classes**: sperm, sperm_head, sperm_tail

### 2. How to Obtain the Model

#### Option 1: Use Pre-trained YOLOv8 and Fine-tune
```bash
pip install ultralytics
python -c "
from ultralytics import YOLO
model = YOLO('yolov8n.pt')
# Fine-tune on your sperm dataset
model.train(data='sperm_dataset.yaml', epochs=100)
# Export to ONNX
model.export(format='onnx')
"
```

#### Option 2: Download Base Model and Convert
```bash
# Download YOLOv8n base model
wget https://github.com/ultralytics/yolov8/releases/download/v8.0.0/yolov8n.pt

# Convert to ONNX (requires ultralytics package)
yolo export model=yolov8n.pt format=onnx
```

#### Option 3: Custom Training Dataset
For best results with CASA analysis, train on a custom dataset containing:

- **Training Images**: 1000-5000 microscopy images of sperm samples
- **Annotations**: Bounding boxes around sperm heads and tails
- **Magnifications**: 400x, 1000x typical for CASA systems
- **Conditions**: Various sample preparations and concentrations

### 3. Dataset Structure for Training
```
sperm_dataset/
├── images/
│   ├── train/
│   ├── val/
│   └── test/
├── labels/
│   ├── train/
│   ├── val/
│   └── test/
└── dataset.yaml
```

### 4. Model Performance Expectations
- **Precision**: >95% for sperm head detection
- **Recall**: >90% for motile sperm detection  
- **Speed**: >30 FPS on modern GPUs
- **Accuracy**: Suitable for WHO 2010 CASA standards

### 5. Alternative Models
If custom training is not available, you can use:
- General object detection models (lower accuracy)
- Pre-trained cell detection models
- Medical image analysis models from research papers

### 6. Model Validation
Before using in production:
- Test on known sperm concentration samples
- Compare results with manual counting
- Validate WHO parameter calculations
- Test on various sample types and conditions

## Troubleshooting

**Model file not found**: Ensure the file is named exactly `yolov8n_sperm.onnx` and placed in this directory.

**Poor detection accuracy**: The base YOLOv8 model is trained on general objects. For medical accuracy, custom training is recommended.

**Performance issues**: Consider using smaller model variants (yolov8s, yolov8n) for faster inference on lower-end hardware.

## Legal and Ethical Considerations
- Ensure proper licensing for any pre-trained models used
- Validate medical accuracy before clinical use
- Follow local regulations for medical device software
- Consider FDA/CE marking requirements for clinical applications